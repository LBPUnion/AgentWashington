using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace LBPUnion.AgentWashington
{
    public sealed class BotApplication
    {
        private DiscordBotConfiguration _config;
        private DiscordSocketClient _client;
        private bool _running;
        private Dictionary<GameServer, ServerStatus> _status = new();
        private List<GameServer> _serversToMonitor = new();
        private bool _isConnected = false;
        private readonly string LocalDataPath = AppDomain.CurrentDomain.BaseDirectory;

        public void RegisterGameServer(string host, bool secure, int port, bool dnsLookup, string requestPath = "/", bool ignoreCertErrors = false)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("Host must not be empty or blank.");

            if (string.IsNullOrWhiteSpace(requestPath))
                requestPath = "/";

            var server = new GameServer
            {
                Host = host,
                PerformDnsLookup = dnsLookup,
                Port = port,
                UseHttps = secure,
                RequestPath = requestPath,
                IgnoreCertificateErrors = ignoreCertErrors
            };

            _serversToMonitor.Add(server);
        }
        
        public void Run()
        {
            ThrowIfRunning();
            ReadConfigOrThrow();

            _running = true;
            
            BeginClientConnection();

            RunBotAsync().GetAwaiter().GetResult();
        }

        private async Task RunBotAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _config.Connection.DiscordBotToken);
            await _client.StartAsync();

            var runtime = TimeSpan.Zero;
            var stopwatch = new Stopwatch();

            var monitorSince = TimeSpan.FromSeconds(_config.Features.MonitorIntervalSeconds);
            
            stopwatch.Start();
            
            while (_running)
            {
                var elapsed = stopwatch.Elapsed;
                var updateTime = elapsed - runtime;

                if (monitorSince.TotalSeconds >= _config.Features.MonitorIntervalSeconds && _isConnected)
                {
                    monitorSince = TimeSpan.Zero;
                    await UpdateMonitorDataAsync();

                    Database.CommitChanges();
                }

                monitorSince += updateTime;
                
                runtime = elapsed;

                await Task.Delay(10);
            }

            stopwatch.Stop();
        }

        private async Task UpdateMonitorDataAsync()
        {
            var postLiveUpdate = false;

            var liveEmbed = new EmbedBuilder();
            liveEmbed.Title = "Current LBP Server Status";
            liveEmbed.Description =
                $"Below is the status of all LBP game servers as of the last {_config.Features.MonitorIntervalSeconds} second(s).";

            foreach (var server in _serversToMonitor)
            {
                var result = await CheckServerStatus(server);

                var hasChanged = !_status.ContainsKey(server) || (!_status[server].IdenticalTo(result));

                if (!_status.ContainsKey(server))
                    _status.Add(server, result);
                else
                    _status[server] = result;
                
                if (_config.Features.EnableLiveMonitor)
                {
                    postLiveUpdate = true;

                    var emoji = result.IsOnline switch
                    {
                        true => ":white_check_mark",
                        false => ":no_entry_sign:",
                        null => ":question:"
                    };

                    liveEmbed.AddField($"{server.Host}:{server.Port}", $"{emoji} {result.ResponseStatusCode}");
                }

                if (hasChanged)
                {
                    postLiveUpdate = true;
                    
                    foreach (var guildId in _config.Guilds.Keys)
                    {
                        var guildSettings = _config.Guilds[guildId];
                        var guild = _client.GetGuild(guildId);

                        var historyChannel = guild.GetTextChannel(guildSettings.MonitorHistoryChannelId);

                        var embed = new EmbedBuilder();
                        embed.Title = $"Server status has changed!";
                        embed.Description = $"{server.Host}:{server.Port}";

                        embed.AddField("Status Code",
                            $"{result.ResponseStatusCode} ({(HttpStatusCode) result.ResponseStatusCode})");

                        embed.AddField("What does this mean?", result.Meaning);
                        
                        embed.AddField("Is it online?", result.IsOnline switch
                        {
                            true => "Yes",
                            false => "No",
                            _ => "We don't know."
                        });

                        await historyChannel.SendMessageAsync(null, false, embed.Build());
                    }
                }
            }

            if (postLiveUpdate && _config.Features.EnableLiveMonitor)
            {
                liveEmbed.WithFooter($"Last updated: {DateTime.UtcNow} (UTC)");
                
                foreach (var guildId in _config.Guilds.Keys)
                {
                    var guildSettings = _config.Guilds[guildId];
                    var guild = _client.GetGuild(guildId);
                    var liveMessage = 0ul;

                    var channel = guild.GetTextChannel(guildSettings.LiveMonitorChannelId);
                    
                    if (!Database.GetGuildLiveMonitorMessageId(guildId, out liveMessage))
                    {
                        var msg = await channel.SendMessageAsync(null, false, liveEmbed.Build());
                        Database.SetGuildLiveMonitorMessageId(guildId, msg.Id);
                        continue;
                    }

                    await channel.ModifyMessageAsync(liveMessage, (props) =>
                    {
                        props.Embed = liveEmbed.Build();
                    });
                }
            }
        }

        private async Task<ServerStatus> CheckServerStatus(GameServer server)
        {
            if (server.IgnoreCertificateErrors)
            {
                ServicePointManager.ServerCertificateValidationCallback += IgnoreCertChecks;
            }

            var meaning = string.Empty;
            var isOnline = (bool?) true;
            var statusCode = 0;

            if (server.PerformDnsLookup)
            {
                try
                {
                    Console.WriteLine("[{0}:{1}] Doing a dns lookup...", server.Host, server.Port);
                    var hosts = Dns.GetHostAddressesAsync(server.Host);
                    Console.WriteLine("[{0}:{1}] Done.", server.Host, server.Port);
                }
                catch
                {
                    Console.WriteLine("[{0}:{1}] Failed. Treating this as if the server's offline, but it's probably an issue on our end.", server.Host, server.Port);
                    meaning =
                        "DNS Error means the bot could not successfully resolve the IP address of this server. Most likely something was wrong with the bot's Internet connection.";
                    isOnline = false;
                }
            }

            if (isOnline == true)
            {
                var url = server.UseHttps ? "https" : "http";
                url += "://" + server.Host + ":" + server.Port.ToString() + server.RequestPath;

                Console.WriteLine("[{0}:{1}] Attempting a {2} GET request on {0}:{1} for the path {3}...", server.Host,
                    server.Port, server.UseHttps ? "HTTPS" : "HTTP", server.RequestPath);

                var webRequest = WebRequest.Create(url);

                try
                {
                    var res = await webRequest.GetResponseAsync() as HttpWebResponse;

                    isOnline = true;

                    statusCode = (int) res.StatusCode;
                    meaning = GetStatusInfo(statusCode);
                    
                }
                catch (WebException ex)
                {
                    var res = ex.Response as HttpWebResponse;
                    
                    statusCode = (int) res.StatusCode;

                    meaning = GetStatusInfo(statusCode);

                    isOnline = GetStatusIsOnline(server, statusCode);
                }

            }

            Console.WriteLine("[{0}:{1}] Finished.", server.Host, server.Port);
            Console.WriteLine("[{0}:{1}] STATUS CODE: {2}", server.Host, server.Port, statusCode);
            Console.WriteLine("[{0}:{1}] MEANING: {2}", server.Host, server.Port, meaning);
            Console.WriteLine("[{0}:{1}] ONLINE STATUS: {2}", server.Host, server.Port, isOnline);

            if (server.IgnoreCertificateErrors)
            {
                ServicePointManager.ServerCertificateValidationCallback -= IgnoreCertChecks;
            }

            return new ServerStatus(server, statusCode, meaning, isOnline);
        }

        private bool IgnoreCertChecks(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslpolicyerrors)
        {
            return true;
        }

        private void ReadConfigOrThrow()
        {
            var configPath = Path.Combine(LocalDataPath, "config.json");
            if (!File.Exists(configPath))
                throw new InvalidOperationException(
                    "Cannot start the bot because no configuration file is present. Please rename the config.example.json file to config.json and fill out the required information.");

            var json = File.ReadAllText(configPath);

            this._config = JsonSerializer.Deserialize<DiscordBotConfiguration>(json);

            if (_config == null)
                throw new InvalidOperationException("Invalid config file: Root json object is null.");
            
            if (_config.Connection == null)
                throw new InvalidOperationException("Invalid config file: Connection settings field is null.");
            if (_config.Features == null)
                throw new InvalidOperationException("Invalid config file: Features object is null.");
        }

        private void BeginClientConnection()
        {
            _client = new DiscordSocketClient();

            Database.Open();
            
            _client.Log += ClientOnLog;
            _client.Ready += ClientOnReady;
        }

        private Task ClientOnReady()
        {
            _isConnected = true;
            return Task.CompletedTask;
        }

        private Task ClientOnLog(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private void ThrowIfRunning()
        {
            if (_running)
                throw new InvalidOperationException("The bot is already running.");
        }
        
        private string GetStatusInfo(int code)
        {
            return code switch
            {
                503 =>
                    "In LittleBigPlanet, this error means that the servers are currently undergoing maintenance. This will show up in-game as Error Code 403, which  also just means an unknown error has occurred. The servers are down but they'll likely be back up in the future.",
                _ =>
                    "This error is unknown in the context of LittleBigPlanet. It's unclear whether the servers are online or not, but most likely, they are down."
            };
        }

        private bool? GetStatusIsOnline(GameServer server, int statusCode)
        {
            return statusCode switch
            {
                200 => true,
                404 => null, // we don't know
                _ => false
            };
        }

    }

    public sealed class GameServer
    {
        public string Host { get; set; }
        public bool PerformDnsLookup { get; set; }
        public bool UseHttps { get; set; }
        public string RequestPath { get; set; }
        public bool IgnoreCertificateErrors { get; set; }
        public int Port { get; set; }
    }

    public class ServerStatus
    {
        public GameServer Server { get; }
        public int ResponseStatusCode { get; }
        public string Meaning { get; }
        public bool? IsOnline { get; }
        public DateTime LastChecked { get; }

        public ServerStatus(GameServer gameServer, int code, string meaning, bool? isOnline)
        {
            Server = gameServer;
            ResponseStatusCode = code;
            IsOnline = isOnline;
            Meaning = meaning;
            LastChecked = DateTime.UtcNow;
        }

        public bool IdenticalTo(ServerStatus otherStatus)
        {
            return this.Server == otherStatus.Server && this.ResponseStatusCode == otherStatus.ResponseStatusCode;
        }
    }
}
using System;
using System.IO;
using LiteDB;

namespace LBPUnion.AgentWashington
{
    public static class Database
    {
        private static LiteDatabase _db;

        public static void CommitChanges()
        {
            _db.Commit();
            Close();
            Open();
        }
        
        public static void Close()
        {
            if (_db == null)
                throw new InvalidOperationException("Database isn't open.");
            _db.Dispose();
            _db = null;
        }
        
        public static void Open()
        {
            if (_db != null)
                throw new InvalidOperationException("Database already open.");

            _db = OpenDatabase();
        }

        public static bool GetLastKnownServerStatus(GameServer server, out ServerStatus serverStatus)
        {
            var statuses = _db.GetCollection<KnownServerStatus>("knownStatuses");

            var knownStatus = statuses.FindOne(x => x.Url == server.Url);
            if (knownStatus == null)
            {
                serverStatus = null;
                return false;
            }
            else
            {
                serverStatus = new ServerStatus(server, knownStatus.StatusCode, knownStatus.Meaning,
                    knownStatus.IsOnline switch
                    {
                        OnlineStatus.Online => true,
                        OnlineStatus.Offline => false,
                        OnlineStatus.Unknown => null
                    }, knownStatus.LastChecked);
                return true;
            }
        }

        public static void SetLastKnownServerStatus(GameServer server, ServerStatus serverStatus)
        {
            var statuses = _db.GetCollection<KnownServerStatus>("knownStatuses");

            var knownStatus = statuses.FindOne(x => x.Url == server.Url);
            if (knownStatus == null)
            {
                knownStatus = new KnownServerStatus
                {
                    Url = server.Url,
                    StatusCode = serverStatus.ResponseStatusCode,
                    IsOnline = serverStatus.IsOnline switch
                    {
                        true => OnlineStatus.Online,
                        false => OnlineStatus.Offline,
                        null => OnlineStatus.Unknown
                    },
                    LastChecked = serverStatus.LastChecked,
                    Meaning = serverStatus.Meaning
                };
                statuses.Insert(knownStatus);
            }
            else
            {
                knownStatus.Url = server.Url;
                knownStatus.StatusCode = serverStatus.ResponseStatusCode;
                knownStatus.IsOnline = serverStatus.IsOnline switch
                {
                    true => OnlineStatus.Online,
                    false => OnlineStatus.Offline,
                    null => OnlineStatus.Unknown
                };
                knownStatus.LastChecked = serverStatus.LastChecked;
                knownStatus.Meaning = serverStatus.Meaning;
                statuses.Update(knownStatus);
            }
        }
        
        public static bool GetGuildLiveMonitorMessageId(ulong guild, out ulong messageId)
        {
            var guildMessages = _db.GetCollection<GuildMessage>("liveMonitorMessages");

            var msg = guildMessages.FindOne(x => x.GuildId == guild);

            if (msg != null)
            {
                messageId = msg.MessageId;
                return true;
            }
            else
            {
                messageId = 0;
                return false;
            }
        }

        public static void SetGuildLiveMonitorMessageId(ulong guild, ulong messageId)
        {
            var guildMessages = _db.GetCollection<GuildMessage>("liveMonitorMessages");

            var msg = guildMessages.FindOne(x => x.GuildId == guild);

            if (msg != null)
            {
                msg.MessageId = messageId;
                guildMessages.Update(msg);
            }
            else
            {
                msg = new GuildMessage
                {
                    GuildId = guild,
                    MessageId = messageId
                };
                guildMessages.Insert(msg);
            }
        }

        private static LiteDatabase OpenDatabase()
        {
            return new LiteDatabase(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.db"));
        }
    }

    public class GuildMessage
    {
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong MessageId { get; set; }
    }

    public class KnownServerStatus
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public int StatusCode { get; set; }
        public OnlineStatus IsOnline { get; set; }
        public DateTime LastChecked { get; set; }
        public string Meaning { get; set; }
    }

    public enum OnlineStatus
    {
        Online,
        Offline,
        Unknown
    }
}
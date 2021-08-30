using System;
using MongoDB.Driver;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace LBPUnion.AgentWashington
{
    public sealed class WebDatabase
    {
        private MongoClient _mongoClient;
        private IMongoDatabase _db;
        private IMongoCollection<WebServerStatus> _statusHistory;
        private IMongoCollection<WebServerInfo> _serverInfo;
        
        public WebDatabase(string connectionString)
        {
            _mongoClient = new(connectionString);

            _db = _mongoClient.GetDatabase("agent-washington");

            _serverInfo = _db.GetCollection<WebServerInfo>("servers");
            _statusHistory = _db.GetCollection<WebServerStatus>("statusHistory");
        }

        private async Task<(bool, ObjectId)> IsServerKnown(GameServer server)
        {
            var knownServerFind = _serverInfo.Find(x => x.Host == $"{server.Host}:{server.Port}");

            if (await knownServerFind.AnyAsync())
            {
                var knownServer = await knownServerFind.SingleAsync();       
                if (knownServer.Name != server.Name)
                {
                    knownServer.Name = server.Name;
                    await _serverInfo.UpdateOneAsync(x => x.Host == knownServer.Host, knownServer.Name);
                }

                return (true, knownServer.Id);
            }

            var newServer = new WebServerInfo
            {
                Name = server.Name,
                Host = $"{server.Host}:{server.Port}",
                Description = server.Description
            };

            await _serverInfo.InsertOneAsync(newServer);

            return (false, newServer.Id);
        }
        
        public async Task ReportServerStatus(ServerStatus serverStatus, bool hasChanged)
        {
            var (isKnown, serverId) = await IsServerKnown(serverStatus.Server);

            if (hasChanged || !isKnown)
            {
                var update = new WebServerStatus
                {
                    ServerId = serverId,
                    StatusCode = serverStatus.ResponseStatusCode,
                    ReportDate = serverStatus.LastChecked,
                    OnlineStatus = serverStatus.IsOnline switch
                    {
                        true => OnlineStatus.Online,
                        false => OnlineStatus.Offline,
                        null => OnlineStatus.Unknown
                    }
                };

                await _statusHistory.InsertOneAsync(update);
            }
        }
    }

    public class WebServerInfo
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Host { get; set; }
    }

    public class WebServerStatus
    {
        public ObjectId Id { get; set; }
        public ObjectId ServerId { get; set; }
        public int StatusCode { get; set; }
        public OnlineStatus OnlineStatus { get; set; }
        public DateTime ReportDate { get; set; }
    }
}
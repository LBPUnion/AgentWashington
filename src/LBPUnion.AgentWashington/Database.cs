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
}
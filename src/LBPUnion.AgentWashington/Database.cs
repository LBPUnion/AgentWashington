using System;
using System.IO;
using LiteDB;

namespace LBPUnion.AgentWashington
{
    public static class Database
    {

        public static bool GetGuildLiveMonitorMessageId(ulong guild, out ulong messageId)
        {
            using var db = OpenDatabase();

            var guildMessages = db.GetCollection<GuildMessage>("liveMonitorMessages");

            var msg = guildMessages.FindOne(x => x.GuildId == guild);

            db.Dispose();

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
            using var db = OpenDatabase();

            var guildMessages = db.GetCollection<GuildMessage>("liveMonitorMessages");

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
            
            db.Dispose();
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
using System.Collections.Generic;

namespace LBPUnion.AgentWashington
{
    internal class DiscordBotConfiguration
    {
        public ConnectionSettings Connection { get; set; } = new();
        public FeatureSettings Features { get; set; } = new();

        public Dictionary<ulong, GuildConfig> Guilds { get; set; } = new();
    }

    public class GuildConfig
    {
        public ulong LiveMonitorChannelId { get; set; }
        public ulong MonitorHistoryChannelId { get; set; }

        public ulong[] AdministratorRoles { get; set; }

        public ulong[] AdministratorUsers { get; set; }
    }
}
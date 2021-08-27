namespace LBPUnion.AgentWashington
{
    public class FeatureSettings
    {
        public bool EnableLiveMonitor { get; set; } = false;
        public bool EnableMonitorHistory { get; set; } = false;
        public double MonitorIntervalSeconds { get; set; } = 30;
        
        public ulong DeveloperUserId { get; set; }
    }
}
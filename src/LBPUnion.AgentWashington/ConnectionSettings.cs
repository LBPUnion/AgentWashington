namespace LBPUnion.AgentWashington
{
    internal class ConnectionSettings
    {
        public string CommandPrefix { get; set; } = "aw:";
        public string DiscordBotToken { get; set; } = "Please enter your Discord bot token here.";
        public string MongoConnectionString { get; set; }
    }
}
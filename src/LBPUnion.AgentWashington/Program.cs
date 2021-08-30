using System;

namespace LBPUnion.AgentWashington
{
    class Program
    {
        static void Main(string[] args)
        {
            // This allows the database layer to shut down and save the changes if the OS pulls the plug on us
            // (e.x, systemctl stop agent-washington when we're running as a systemd service)
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            
#if !DEBUG
            try 
            {
#endif
                var bot = new BotApplication();
                
                // this is where we register servers to monitor.
                bot.RegisterGameServer(
                        "Mainline Game Servers", // server name,
                        "Main LittleBigPlanet game server for LBP 1, 2, and 3.",
                    "littlebigplanetps3.online.scee.com", // Mainline game server
                    true, // Use HTTPS instead of HTTP
                    10061, // Gameserver HTTPS port
                    true, // Perform a DNS lookup before we do anything else
                    "/", // Request path.
                    true // Ignore certificate errors.
                );
                
                bot.Run();
#if !DEBUG
            }
            catch (Exception ex)
            {
                Console.WriteLine("Oh no! The bot has crashed.");
                Console.WriteLine();
                Console.WriteLine(ex);
            }
#endif
        }

        private static void CurrentDomainOnProcessExit(object? sender, EventArgs e)
        {
            Database.Close();
        }
    }
}
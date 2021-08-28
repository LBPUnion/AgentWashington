using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace LBPUnion.AgentWashington
{
    class Program
    {
        static void Main(string[] args)
        {
#if !DEBUG
            try 
            {
#endif
                var bot = new BotApplication();
                
                // this is where we register servers to monitor.
                bot.RegisterGameServer(
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
    }
}
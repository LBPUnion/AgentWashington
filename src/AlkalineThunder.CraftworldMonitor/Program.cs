using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace AlkalineThunder.CraftworldMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback += ServerCertificateValidationCallback;
            
            var serverName = "littlebigplanetps3.online.scee.com";

            var port = 10061;
            
            Console.WriteLine("Performing DNS lookup on {0}...", serverName);

            var hosts = Array.Empty<IPAddress>();

            try
            {
                hosts = Dns.GetHostAddresses(serverName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DNS lookup failed. Here's why.");
                Console.WriteLine();
                Console.WriteLine(ex);
                Console.WriteLine();
                Console.WriteLine("Most likely this means you're not connected to the Internet or something's wrong on your end. This doesn't mean the LBP servers are down, it means we cannot find them. Check your internet and try again.");

                return;
            }

            var host = hosts.First().ToString();

            Console.WriteLine("DNS LOOKUP SUCCEEDED: IP is {0}", host);

            var url = "https://" + serverName + ":" + port.ToString() + "/";
            
            Console.WriteLine("Sending an HTTPS GET request to {0} to see if the server responds.", url);

            try
            {
                var webRequest = WebRequest.Create(url);
                using var response = webRequest.GetResponse();
                using var responseStream = response.GetResponseStream();
                using var reader = new StreamReader(responseStream);
                var responseText = reader.ReadToEnd();
                Console.WriteLine("Response body:");
                Console.WriteLine(responseText);
            }
            catch (WebException wex)
            {
                var res = wex.Response as HttpWebResponse;
                var status = res.StatusCode;
                var statusCode = (int) status;

                Console.WriteLine("HTTP request failed with status code {0} ({1})", statusCode, status);
                Console.WriteLine();
                Console.WriteLine(GetStatusInfo(status));
                Console.WriteLine();
                Console.WriteLine(wex);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static bool ServerCertificateValidationCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslpolicyerrors)
        {
            return true;
        }

        private static string GetStatusInfo(HttpStatusCode code)
        {
            return code switch
            {
                HttpStatusCode.ServiceUnavailable =>
                    "In LittleBigPlanet, this error means that the servers are currently undergoing maintenance. This will show up in-game as Error Code 403, which  also just means an unknown error has occurred. The servers are down but they'll likely be back up in the future.",
                _ =>
                    "This error is unknown in the context of LittleBigPlanet. It's unclear whether the servers are online or not, but most likely, they are down."
            };
        }
    }
}
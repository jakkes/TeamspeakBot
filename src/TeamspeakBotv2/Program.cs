using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamspeakBotv2.Config;
using System.IO;
using Newtonsoft.Json;
using TeamspeakBotv2.Core;
using System.Threading;

namespace TeamspeakBotv2
{
    public class Program
    {
        static string ConfigFilePath = "config.cnf";
        static List<Host> hosts = new List<Host>();
        static Timer tmr;
        public static void Main(string[] args)
        {
            Console.WriteLine("Welcome.");
            var cnf = LoadConfig();
            foreach(var host in cnf)
            {
                hosts.Add(new Host(host));
            }
            tmr = new Timer(new TimerCallback(UpdateConfig), null, 300000, 120000);
            while(Console.ReadLine() != "exit")
            {

            }
        }

        private static void UpdateConfig(object state)
        {
            var cnf = LoadConfig();
            foreach(var host in cnf)
            {
                Host h;
                if((h = hosts.FirstOrDefault(x => x.Endpoint.Address.ToString() == host.Host)) != null)
                {
                    h.UpdateConfig(host);
                }
            }
        }

        public static HostConfig[] LoadConfig()
        {
            try
            {
                return JsonConvert.DeserializeObject<HostConfig[]>(File.ReadAllText(ConfigFilePath));
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Could not find configuration file.");
                throw;
            }
        }
    }
}

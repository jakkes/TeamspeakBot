using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamspeakBotv2.Config;
using System.IO;
using Newtonsoft.Json;
using TeamspeakBotv2.Core;

namespace TeamspeakBotv2
{
    public class Program
    {
        static string ConfigFilePath = "config.cnf";
        static List<Host> hosts = new List<Host>();
        public static void Main(string[] args)
        {
            Console.WriteLine("Welcome.");
            var cnf = LoadConfig();
            foreach(var host in cnf)
            {
                hosts.Add(new Host(host));
            }
            Console.ReadLine();
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

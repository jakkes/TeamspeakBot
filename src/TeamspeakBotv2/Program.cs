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
            Console.WriteLine("Welcome. Version 2.0");


            // Start hosts
            HostConfig[] cnf;
            try { cnf = LoadConfig(); }
            catch (FileNotFoundException) { Console.WriteLine("Configuration file was not found."); Console.Read(); return; }
            catch (JsonReaderException ex) { Console.WriteLine("Failed parsing the configuration file."); Console.WriteLine(ex.Message); return; }
            StartRange(cnf);

            // Start update timer
            tmr = new Timer(new TimerCallback(UpdateConfig), null, 300000, 300000);

            // Loop until exit command
            while (true)
            {
                var s = Console.ReadLine();
                if (s.ToLower() == "exit")
                    break;
                else if (s.ToLower() == "update")
                    UpdateConfig(null);
            }

            // Shutdown
            foreach(var host in hosts)
                host.Dispose();
        }
        /// <summary>
        /// Starts hosts
        /// </summary>
        /// <param name="cnf">Hosts to start.</param>
        static void StartRange(HostConfig[] cnf)
        {
            foreach (var host in cnf)
            {
                try { Start(host); }
                catch(Exception ex) { Console.WriteLine("Failed to start host."); Console.WriteLine(ex.Message); }
            }
        }
        /// <summary>
        /// Starts a host
        /// </summary>
        /// <param name="cnf">Host config</param>
        static void Start(HostConfig cnf)
        {
            var h = new Host(cnf);
            h.Disposed += Host_Disposed;
            hosts.Add(h);
        }
        private static void Host_Disposed(object sender, EventArgs e)
        {
            hosts.Remove((Host)sender);
        }
        private static void UpdateConfig(object state)
        {
            Console.WriteLine("Updating config...");

            HostConfig[] cnf;
            try { cnf = LoadConfig(); }
            catch (FileNotFoundException) { Console.WriteLine("Configuration file is missing. Failed to update."); return; }
            catch (JsonReaderException ex) { Console.WriteLine("Failed parsing the configuration file."); Console.WriteLine(ex.Message); return; }

            var currHosts = hosts.ToArray();
            var exists = new bool[currHosts.Length];
            hosts = new List<Host>();

            foreach(var host in cnf)
            {
                // Check if host is running
                bool found = false;
                for(int i = 0; i < currHosts.Length; i++)
                {
                    if(host.Host == currHosts[i].Endpoint.Address.ToString() && host.Port == currHosts[i].Endpoint.Port)
                    {
                        found = true;
                        exists[i] = true;
                        // Update host
                        currHosts[i].UpdateConfig(host);
                        break;
                    }
                }
                // If not running, start it.
                if (!found)
                    try { Start(host); }
                    catch(Exception ex) { Console.WriteLine("Failed to start host on update."); Console.WriteLine(ex.Message); }

                Console.WriteLine("Update done.");
            }

            // Update list
            for(int i = 0; i < currHosts.Length; i++)
            {
                if (exists[i])
                    hosts.Add(currHosts[i]);
                else
                    currHosts[i].Dispose();
            }
        }
        /// <summary>
        /// Loads the configuration file.
        /// </summary>
        /// <returns>Configuration</returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static HostConfig[] LoadConfig()
        {
            return JsonConvert.DeserializeObject<HostConfig[]>(File.ReadAllText(ConfigFilePath));
        }
    }
}

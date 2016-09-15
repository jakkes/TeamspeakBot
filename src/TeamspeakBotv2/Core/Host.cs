using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TeamspeakBotv2.Config;

namespace TeamspeakBotv2.Core
{
    public class Host
    {
        private HostConfig config;
        private List<Server> servers = new List<Server>();
        public Host(HostConfig cnf)
        {
            config = cnf;
            StartServers();
        }

        private void StartServers()
        {
            IPEndPoint Host;

            try
            {
                Host = new IPEndPoint(IPAddress.Parse(config.Host), config.Port);
            }
            catch (FormatException)
            {
                Console.WriteLine("Could not parse " + config.Host + " to an IP address");
                throw;
            }

            foreach (var server in config.Servers)
                servers.Add(new Server(server, Host, config.Username, config.Password, config.Timeout));
        }
    }
}

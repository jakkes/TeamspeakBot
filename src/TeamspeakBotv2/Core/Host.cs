using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TeamspeakBotv2.Config;

namespace TeamspeakBotv2.Core
{
    public class Host : IDisposable
    {
        private HostConfig config;
        public event EventHandler Disposed;
        public IPEndPoint Endpoint { get; private set; }
        private List<Server> servers = new List<Server>();
        public Host(HostConfig cnf)
        {
            config = cnf;
            try
            {
                Endpoint = new IPEndPoint(IPAddress.Parse(config.Host), config.Port);
            }
            catch (FormatException)
            {
                Console.WriteLine("Could not parse " + config.Host + " to an IP address");
                throw;
            }
            StartServers();
        }


        public void UpdateConfig(HostConfig cnf)
        {
            config = cnf;
            foreach(var srv in cnf.Servers)
            {
                Server s;
                if((s = servers.FirstOrDefault(x => x.ServerId == srv.Id)) != null)
                {
                    s.UpdateConfig(srv);
                }
                else
                {
                    StartServer(srv);
                }
            }
        }
        private void StartServer(ServerConfig server)
        {
            var srv = new Server(server, Endpoint, config.Username, config.Password, config.Timeout);
            srv.Disposed += Srv_Disposed;
            lock (servers)
                servers.Add(srv);
        }
        private void StartServers()
        {

            foreach (var server in config.Servers)
                StartServer(server);
        }

        private void Srv_Disposed(object sender, EventArgs e)
        {
            lock (servers)
                servers.Remove((Server)sender);
        }

        public void Dispose()
        {
            lock (servers)
            {
                foreach(var server in servers)
                {
                    server.Disposed -= Srv_Disposed;
                    server.Dispose();
                }
            }
        }
    }
}

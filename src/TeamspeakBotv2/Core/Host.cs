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
                Dispose();
                return;
            }
            StartServers();
        }


        public void UpdateConfig(HostConfig cnf)
        {
            config = cnf;

            var currServers = servers.ToArray();
            var exists = new bool[currServers.Length];
            servers = new List<Server>();
            
            foreach(var srv in cnf.Servers)
            {
                bool found = false;
                for(int i = 0; i < currServers.Length; i++)
                {
                    if(currServers[i].ServerId == srv.Id)
                    {
                        found = true;
                        exists[i] = true;
                        currServers[i].UpdateConfig(srv, config.Timeout);
                        break;
                    }
                }
                if (!found)
                    try { StartServer(srv); }
                    catch(Exception ex) { Console.WriteLine("Failed to start server on update."); Console.WriteLine(ex.Message); }
            }

            for(int i = 0; i < currServers.Length; i++)
            {
                if (exists[i])
                    servers.Add(currServers[i]);
                else
                    currServers[i].Dispose();
            }
        }
        private void StartServer(ServerConfig server)
        {
            var srv = new Server(server, Endpoint, config.Username, config.Password, config.Timeout);
            srv.Disposed += Srv_Disposed;
            servers.Add(srv);
        }
        private void StartServers()
        {
            foreach (var server in config.Servers)
                try { StartServer(server); }
                catch(Exception ex) { Console.WriteLine("Failed to start server."); Console.WriteLine(ex.Message); }
        }

        private void Srv_Disposed(object sender, EventArgs e)
        {
            servers.Remove((Server)sender);
        }

        public void Dispose()
        {
            foreach (var server in servers)
            {
                server.Disposed -= Srv_Disposed;
                server.Dispose();
            }
            Disposed?.Invoke(this, null);
        }
    }
}

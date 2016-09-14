using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TeamspeakBotv2.Config;

namespace TeamspeakBotv2.Core
{
    public class Server : IDisposable
    {
        public event EventHandler Disposed;
        private ServerConfig config;
        public IPEndPoint Host { get; private set; }
        private string Username;
        private string Password;
        private List<Channel> Channels = new List<Channel>();
        public Server(ServerConfig cnf, IPEndPoint host, string username, string password)
        {
            config = cnf;
            Host = host;
            Username = username;
            Password = password;
        }
        private void ChannelDisposed(object sender, EventArgs e)
        {
            lock(Channels)
                Channels.Remove((Channel)sender));
        }
        public void Dispose()
        {
            foreach (var channel in Channels)
            {
                channel.Disposed -= ChannelDisposed;
                channel.Dispose();
            }
            if (Disposed != null)
                Disposed(this, new EventArgs());
        }
    }
}

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
        private int Timeout;

        public Server(ServerConfig cnf, IPEndPoint host, string username, string password, int timeout)
        {
            config = cnf;
            Host = host;
            Username = username;
            Password = password;
            Timeout = timeout;
            StartChannels();
        }

        public void StartChannels()
        {
            foreach (var ch in config.Channels)
            {
                if (!Channels.Any(x => x.ChannelName == ch))
                {
                    try
                    {
                        var cha = new Channel(ch, config.DefaultChannel, Host, Username, Password, config.Id, Timeout);
                        Channels.Add(cha);
                    } catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }
        private void ChannelDisposed(object sender, EventArgs e)
        {
            lock (Channels)
            {
                Channels.Remove((Channel)sender);
                if (Channels.Count == 0)
                    Dispose();
            }
        }
        public void Dispose()
        {
            foreach (var channel in Channels)
            {
                channel.Disposed -= ChannelDisposed;
                channel.Dispose();
            }
            Disposed?.Invoke(this, new EventArgs());
        }
    }
}

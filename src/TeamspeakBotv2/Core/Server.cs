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
        public int ServerId { get { return config.Id; } }
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

        public void UpdateConfig(ServerConfig cnf)
        {
            config = cnf;
            Channels.Where(x => !config.Channels.Any(y => x.ChannelName == y)).ToList().ForEach(x => x.Dispose());
            config.Channels.Where(x => !Channels.Any(y => x == y.ChannelName)).ToList().ForEach(x => StartChannel(x));
        }

        private void StartChannels()
        {
            foreach (var ch in config.Channels)
            {
                StartChannel(ch);
            }
        }
        private void StartChannel(string name)
        {
            if (!Channels.Any(x => x.ChannelName == name))
            {
                try
                {
                    var cha = new Channel(name, config.DefaultChannel, Host, Username, Password, config.Id, Timeout);
                    Channels.Add(cha);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        private void ChannelDisposed(object sender, EventArgs e)
        {
            lock (Channels)
            {
                Channels.Remove((Channel)sender);
            }
            StartChannel(((Channel)sender).ChannelName);
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

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
            List<Channel> newchannels = new List<Channel>();
            foreach(var channel in cnf.Channels)
            {
                Channel ch;
                if ((ch = Channels.FirstOrDefault(x => x.ChannelName == channel)) != null)
                {
                    if (ch.ConnectedAndActive)
                    {
                        newchannels.Add(ch);
                        Channels.Remove(ch);
                    }
                    else
                    {
                        ch.Dispose();
                        StartChannel(ch.ChannelName);
                    }
                } else
                {
                    StartChannel(channel);
                }
            }
            Channels = newchannels;
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
                    cha.Disposed += ChannelDisposed;
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
            Console.WriteLine("Channel " + ((Channel)sender).ChannelName + " stopped.");
            Channels.Remove((Channel)sender);
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

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
        private List<Channel> chs = new List<Channel>();
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

            var currChs = chs.ToArray();
            var exists = new bool[currChs.Length];
            chs = new List<Channel>();

            foreach(var ch in cnf.Channels)
            {
                bool found = false;
                for(int i = 0; i < currChs.Length; i++)
                {
                    if(currChs[i].Active && currChs[i].ChannelName == ch)
                    {
                        found = true;
                        exists[i] = true;
                        break;
                    }
                }
                if (!found)
                    try { StartChannel(ch); }
                    catch(Exception ex) { Console.WriteLine("Failed to start channel on update."); Console.WriteLine(ex.Message); }
            }

            for(int i = 0; i < currChs.Length; i++)
            {
                if (exists[i])
                    chs.Add(currChs[i]);
                else
                    currChs[i].Dispose();
            }
        }
        private void StartChannels()
        {
            foreach (var ch in config.Channels)
            {
                try { StartChannel(ch); }
                catch (Exception ex) { Console.WriteLine("Failed to start channel."); Console.WriteLine(ex.Message); }
            }
        }
        private void StartChannel(string name)
        {
            var cha = new Channel(name, config.Parent, config.DefaultChannel, Host, Username, Password, config.Id, Timeout, config.BanTime);
            cha.Disposed += ChannelDisposed;
            chs.Add(cha);
        }
        private void ChannelDisposed(object sender, EventArgs e)
        {
            Console.WriteLine("Channel " + ((Channel)sender).ChannelName + " stopped.");
            chs.Remove((Channel)sender);
        }
        public void Dispose()
        {
            foreach (var channel in chs)
            {
                channel.Disposed -= ChannelDisposed;
                channel.Dispose();
            }
            Disposed?.Invoke(this, new EventArgs());
        }
    }
}
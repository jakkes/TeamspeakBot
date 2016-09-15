using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using TeamspeakBotv2.Models;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace TeamspeakBotv2.Core
{
    public class Channel : IDisposable
    {
        public event EventHandler Disposed;
        private AutoResetEvent ErrorLineReceived = new AutoResetEvent(false);
        private AutoResetEvent WhoAmIReceived = new AutoResetEvent(false);
        private AutoResetEvent ChannelListUpdated = new AutoResetEvent(false);
        private AutoResetEvent ClientUniqueIdFromClidReceived = new AutoResetEvent(false);
        public string ChannelName { get { return ThisChannel.ChannelName; } }
        public int ChannelId { get { return ThisChannel.ChannelId; } }
        private ChannelModel ThisChannel;
        private ChannelModel DefaultChannel;
        private int Timeout;

        private Socket connection;

        private string OwnerUid;
        private List<string> Banlist;
        private List<string> Whitelist;
        private bool useWhitelist;

        private WhoAmIModel Me;
        private ChannelModel[] ChannelList;
        private List<GetUidFromClidModel> UidFromClidResponses = new List<GetUidFromClidModel>();

        private Timer readTimer;

        public Channel(string channel, string defaultchannel, IPEndPoint host, string username, string password, int serverId, int timeout)
        {
            Timeout = timeout;
            connection = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                connection.Connect(host);
            } catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            readTimer = new Timer(new TimerCallback(Read), null, 0, 250);
            Login(username,password,defaultchannel,channel,serverId);
        }
        
        private void Login(string username, string password, string defaultChannel, string channel, int serverId)
        {
            Send(string.Format("login {0} {1}", username, password));
            if (ErrorLineReceived.WaitOne(Timeout))
            {
                Send(string.Format("use sid={0}", serverId));
                if (ErrorLineReceived.WaitOne(Timeout))
                {
                    var m = WhoAmI();
                    ThisChannel = GetChannel(channel);
                    DefaultChannel = GetChannel(defaultChannel);
                    MoveClient(m, ThisChannel);
                    return;
                }
            }

            throw new Exception("Error when logging in");

        }
        private void Reset()
        {
            OwnerUid = string.Empty;
            Banlist = new List<string>();
            Whitelist = new List<string>();
            useWhitelist = false;
        }
        private WhoAmIModel WhoAmI()
        {
            Send("whoami");
            if (Me == null)
                WhoAmIReceived.WaitOne();
            return Me;
        }
        private ChannelModel GetChannel(int cid)
        {
            if (ChannelList == null)
                UpdateChannelList();
            ChannelModel re;
            if ((re = ChannelList.FirstOrDefault(x => x.ChannelId == cid)) != null)
                return re;
            else
            {
                UpdateChannelList();
                if ((re = ChannelList.FirstOrDefault(x => x.ChannelId == cid)) != null)
                    return re;
                else throw new Exception("There is no channel with id: " + cid.ToString());
            }
        }
        private ChannelModel GetChannel(string name)
        {
            if (ChannelList == null)
                UpdateChannelList();
            ChannelModel re;
            if ((re = ChannelList.FirstOrDefault(x => x.ChannelName == name)) != null)
                return re;
            else
            {
                UpdateChannelList();
                if ((re = ChannelList.FirstOrDefault(x => x.ChannelName == name)) != null)
                    return re;
                else throw new Exception("There is no channel with name: " + name);
            }
        }
        private string GetUniqueId(int clid)
        {
            Send(string.Format("clientgetuidfromclid clid={0}", clid));
            int count = 0;
            while(ClientUniqueIdFromClidReceived.WaitOne(Timeout) && count++ < 3)
            {
                GetUidFromClidModel re;
                if((re = UidFromClidResponses.FirstOrDefault(x => x.ClientId == clid)) != null)
                {
                    lock (UidFromClidResponses)
                        UidFromClidResponses.Remove(re);
                    return re.ClientUniqueId;
                }
            }
            throw new Exception("Get unique id did not return a value for client id: " + clid);
        }
        private void MoveClient(IUser user, ChannelModel targetChannel)
        {
            Send(string.Format("clientmove clid={0} cid={1}", user.ClientId, targetChannel.ChannelId));
            if (!ErrorLineReceived.WaitOne(Timeout))
                throw new Exception("Failed to move client");
        }
        private void UpdateChannelList()
        {
            Send("channellist");
            if (!ChannelListUpdated.WaitOne(Timeout))
                throw new Exception("Channellist failed to receive a reply");
        }
        private void Send(string message)
        {
            connection.SendTo(Encoding.ASCII.GetBytes(message + "\r\n"), connection.RemoteEndPoint);
        }
        private void Read(object state)
        {
            if (!connection.Connected)
            {
                Dispose();
                return;
            }

            byte[] buffer = new byte[4096];
            string msg = string.Empty;
            while(connection.Available != 0)
            {
                int bytes = connection.Receive(buffer);
                msg += Encoding.ASCII.GetString(buffer, 0, bytes);
            }
            string[] msgs = msg.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < msgs.Length; i++)
                HandleReply(msgs[i]);
        }
        private void HandleReply(string line)
        {
            if (line.StartsWith("notifytextmessage"))
                HandleMessage(line);
            else if (line.StartsWith("notifyclientmoved"))
                HandleClientMoved(line);
            else if (line.StartsWith("notifyclientleftview"))
                HandleClientLeftView(line);
            else if (line.StartsWith("notifycliententerview"))
                HandleClientEnterView(line);
            else if (line.StartsWith("error"))
                HandleErrorMessage(line);
            else
            {
                Match m;
                if((m = RegPatterns.ClientUniqueIdFromId.Match(line)).Success)
                {
                    UidFromClidResponses.Add(new GetUidFromClidModel(m));
                }
                else if((m = RegPatterns.Channel.Match(line)).Success)
                {
                    List<ChannelModel> ch = new List<ChannelModel>();
                    ch.Add(new ChannelModel(m));
                    var chs = line.Split('|');
                    for (int i = 1; i < chs.Length; i++)
                        ch.Add(new ChannelModel(RegPatterns.Channel.Match(chs[i])));
                    ChannelList = ch.ToArray();
                    ChannelListUpdated.Set();
                }
                else if((m = RegPatterns.WhoAmI.Match(line)).Success)
                {
                    Me = new WhoAmIModel(m);
                    WhoAmIReceived.Set();
                }
            }
        }
        private void HandleErrorMessage(string line)
        {
            var match = RegPatterns.ErrorLine.Match(line);
            if (match.Success)
            {
                var error = new ErrorModel(match);
                if (error.Id != 0)
                    throw new Exception(error.Message);
                ErrorLineReceived.Set();
            }
        }
        private void HandleClientEnterView(string line)
        {
            Match m = RegPatterns.EnterView.Match(line);
            if (m.Success)
            {
                var model = new ClientEnteredViewModel(m);
            }
        }
        private void HandleClientLeftView(string line)
        {
            throw new NotImplementedException();
        }
        private void HandleClientMoved(string line)
        {
            throw new NotImplementedException();
        }
        private void HandleMessage(string line)
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            connection.Dispose();
            if (Disposed != null)
                Disposed(this, new EventArgs());
        }
    }
}

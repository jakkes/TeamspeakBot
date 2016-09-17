using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using TeamspeakBotv2.Models;
using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Core
{
    public class Channel : IDisposable
    {
        public event EventHandler Disposed;
        private AutoResetEvent ErrorLineReceived = new AutoResetEvent(false);
        private AutoResetEvent WhoAmIReceived = new AutoResetEvent(false);
        private AutoResetEvent ChannelListUpdated = new AutoResetEvent(false);
        private AutoResetEvent ClientListUpdated = new AutoResetEvent(false);
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
        private ClientModel[] ClientList;
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
                    RegisterToEvents();
                    return;
                }
            }

            throw new Exception("Error when logging in");

        }

        private void RegisterToEvents()
        {
            Send("servernotifyregister event=channel id=" + ThisChannel.ChannelId);
            if (!ErrorLineReceived.WaitOne(Timeout))
                throw new Exception("Failed to register to events");
            Send("servernotifyregister event=textchannel");
            if (!ErrorLineReceived.WaitOne(Timeout))
                throw new Exception("Failed to register to events");
        }

        private void Reset()
        {
            OwnerUid = string.Empty;
            Banlist = new List<string>();
            Whitelist = new List<string>();
            useWhitelist = false;
            SendTextMessage("This channel is now unclaimed. To claim possession type !claim.");
        }
        private bool isOwner(ClientModel client)
        {
            if (string.IsNullOrEmpty(client.UniqueId))
                client.UniqueId = GetUniqueId(client);
            return client.UniqueId == OwnerUid;
        }
        private bool isBanned(ClientModel client)
        {
            if (string.IsNullOrEmpty(client.UniqueId))
                client.UniqueId = GetUniqueId(client);
            return !useWhitelist && Banlist.Contains(client.UniqueId);
        }
        private bool isOnWhitelist(ClientModel client)
        {
            if (string.IsNullOrEmpty(client.UniqueId))
                client.UniqueId = GetUniqueId(client);
            return useWhitelist && Whitelist.Contains(client.UniqueId);
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
        private string GetUniqueId(ClientModel client) => GetUniqueId(client.ClientId);
        private void MoveClient(IUser user, ChannelModel targetChannel)
        {
            Send(string.Format("clientmove clid={0} cid={1}", user.ClientId, targetChannel.ChannelId));
            if (!ErrorLineReceived.WaitOne(Timeout))
                throw new Exception("Failed to move client");
        }
        private void PokeClient(IUser user, string message)
        {
            Send(string.Format("clientpoke clid={0} msg={1}", user.ClientId, message.Replace(" ", "\\s")));
        }
        private ClientModel GetClient(string name)
        {
            if (ClientList == null)
                UpdateClientList();
            ClientModel m;
            if ((m = ClientList.FirstOrDefault(x => x.ClientName.ToLower() == name.ToLower())) != null)
                return m;
            else
            {
                UpdateClientList();
                if ((m = ClientList.FirstOrDefault(x => x.ClientName.ToLower() == name.ToLower())) != null)
                    return m;
                else throw new Exception("Could not find user " + name);
            }
        }
        private ClientModel GetClient(int clid)
        {
            if (ClientList == null)
                UpdateClientList();
            ClientModel m;
            if ((m = ClientList.FirstOrDefault(x => x.ClientId == clid)) != null)
                return m;
            else
            {
                UpdateClientList();
                if ((m = ClientList.FirstOrDefault(x => x.ClientId == clid)) != null)
                    return m;
                else throw new Exception("Could not find user with id " + clid);
            }
        }
        private void UpdateChannelList()
        {
            Send("channellist");
            if (!ChannelListUpdated.WaitOne(Timeout))
                throw new Exception("Channellist failed to receive a reply");
        }
        private void UpdateClientList()
        {
            Send("clientlist");
            if (!ClientListUpdated.WaitOne(Timeout))
                throw new Exception("Clientlist failed to receive a reply");
        }
        private void Send(string message)
        {
            connection.SendTo(Encoding.ASCII.GetBytes(message + "\r\n"), connection.RemoteEndPoint);
        }
        private void SendTextMessage(string message)
        {
            Send(string.Format("sendtextmessage msg=\n{0}", message.Replace(" ", "\\s")));
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
            Match m;
            if ((m = RegPatterns.ErrorLine.Match(line)).Success)
            {
                HandleErrorMessage(new ErrorModel(m));
            } else if ((m = RegPatterns.TextMessage.Match(line)).Success)
            {
                HandleMessage(new MessageModel(m));
            }
            else if ((m = RegPatterns.ClientMoved.Match(line)).Success)
            {
                HandleClientMoved(new ClientMovedModel(m));
            } else if ((m = RegPatterns.ClientLeftView.Match(line)).Success)
            {
                HandleClientLeftView(new ClientLeftViewModel(m));
            } else if ((m = RegPatterns.ClientEnteredView.Match(line)).Success)
            {
                HandleClientEnterView(new ClientEnteredViewModel(m));
            }
            else if ((m = RegPatterns.ClientUniqueIdFromId.Match(line)).Success)
            {
                UidFromClidResponses.Add(new GetUidFromClidModel(m));
            }
            else if ((m = RegPatterns.Client.Match(line)).Success)
            {
                List<ClientModel> ch = new List<ClientModel>();
                ch.Add(new ClientModel(m));
                var chs = line.Split('|');
                for (int i = 1; i < chs.Length; i++)
                    ch.Add(new ClientModel(RegPatterns.Channel.Match(chs[i])));
                ClientList = ch.ToArray();
                ClientListUpdated.Set();
            }
            else if ((m = RegPatterns.Channel.Match(line)).Success)
            {
                List<ChannelModel> ch = new List<ChannelModel>();
                ch.Add(new ChannelModel(m));
                var chs = line.Split('|');
                for (int i = 1; i < chs.Length; i++)
                    ch.Add(new ChannelModel(RegPatterns.Channel.Match(chs[i])));
                ChannelList = ch.ToArray();
                ChannelListUpdated.Set();
            }
            else if ((m = RegPatterns.WhoAmI.Match(line)).Success)
            {
                Me = new WhoAmIModel(m);
                WhoAmIReceived.Set();
            }
        }
        private void HandleErrorMessage(ErrorModel model)
        {
            if (model.Id != 0)
                throw new Exception(model.Message);
            ErrorLineReceived.Set();
        }
        private void HandleClientEnterView(ClientEnteredViewModel model)
        {
            ClientModel client = GetClient(model.ClientId);
            if (isBanned(client))
            {
                MoveClient(client, DefaultChannel);
                PokeClient(client, "You are banned from this channel.");
            } else if (!isOnWhitelist(client))
            {
                MoveClient(client, DefaultChannel);
                PokeClient(client, "You are not on the whitelist for this channel.");
            }
        }
        private void HandleClientLeftView(ClientLeftViewModel model)
        {
            ClientModel client = GetClient(model.ClientId);
            if (string.IsNullOrEmpty(client.UniqueId))
                client.UniqueId = GetUniqueId(client);
            if(client.UniqueId == OwnerUid)
            {
                Reset();
            }
        }
        private void HandleClientMoved(ClientMovedModel model)
        {
            ClientModel client = GetClient(model.ClientId);
            client.ChannelId = model.ChannelToId;
            if (model.ChannelToId != ThisChannel.ChannelId)
                if (isOwner(client))
                    Reset();
            else
            {
                if (isBanned(client))
                {
                    MoveClient(client, DefaultChannel);
                    PokeClient(client, "You are banned from this channel.");
                }
                else if (!isOnWhitelist(client))
                {
                    MoveClient(client, DefaultChannel);
                    PokeClient(client, "You are not on the whitelist for this channel.");
                }
            }
        }
        private void DisplayHelp(){
            SendTextMessage("I am a Teamspeakbot here to control the server.");
        }
        private void BanListCommand(MessageModel model){

        }
        private void HandleMessage(MessageModel model)
        {
            if (model.Words[0].StartsWith("!"))
            {
                if(model.Words[0] == "!help")
                    DisplayHelp();
                else if(model.Words[0] == "!cmdlist")
                    DisplayCommandList();
                else if(model.ClientUniqueId == OwnerUid){

                    if(model.Words[0] == "!kick"){
                        
                    }

                }
            }
        }

        private void DisplayCommandList()
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

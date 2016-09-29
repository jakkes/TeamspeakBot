using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using TeamspeakBotv2.Models;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TeamspeakBotv2.Core
{
    public class Channel : IDisposable
    {
        public event EventHandler Disposed;
        public bool ConnectedAndActive
        {
            get
            {
                try
                {
                    return WhoAmI().ChannelId == ThisChannel.ChannelId && connection.Connected;
                } catch (Exception) { return false; }
            }
        }
        private AutoResetEvent ErrorLineReceived = new AutoResetEvent(false);
        private AutoResetEvent WhoAmIReceived = new AutoResetEvent(false);
        private AutoResetEvent ChannelListUpdated = new AutoResetEvent(false);
        private AutoResetEvent ClientListUpdated = new AutoResetEvent(false);
        private AutoResetEvent ClientUniqueIdFromClidReceived = new AutoResetEvent(false);
        private AutoResetEvent DetailedClientReceived = new AutoResetEvent(false);
        public string ChannelName { get { return RealChannelName; } }
        public int ChannelId { get { return ThisChannel.ChannelId; } }
        private ChannelModel ThisChannel;
        private ChannelModel DefaultChannel;
        private string RealChannelName;
        private int Timeout;

        private Socket connection;

        private ClientModel Owner = null;
        private Config config = new Config();

        private WhoAmIModel Me;
        private ChannelModel[] ChannelList;
        private List<ClientModel> ClientList = new List<ClientModel>();
        private List<GetUidFromClidModel> UidFromClidResponses = new List<GetUidFromClidModel>();
        private List<DetailedClientModel> DetailedClientResponses = new List<DetailedClientModel>();

        private Timer readTimer;
        private Timer loopTimer;

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
            readTimer = new Timer(new TimerCallback(Read), null, 0, 100);
            Login(username,password,defaultchannel,channel,serverId);
            loopTimer = new Timer(new TimerCallback((object state) =>
            {
                if (Owner == null)
                    DisplayHelp();
                try
                {
                    if (Owner != null)
                    {
                        var m = GetClient(Owner.ClientId);
                        if (m.ChannelId != ThisChannel.ChannelId)
                            throw new Exception();
                    }
                }
                catch (Exception) { Reset(); }
            }), null, 5000, 300000);
            Console.WriteLine("Starting bot in " + channel);
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
                    RealChannelName = channel;
                    DefaultChannel = GetChannel(defaultChannel);
                    MoveClient(m, ThisChannel);
                    RegisterToEvents();
                    return;
                }
                throw new Exception("Could not select server");
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
            SetChannelName(RealChannelName);
            Owner = null;
            config = new Config();
        }
        private bool isOwner(ClientModel client)
        {
            return client.UniqueId == Owner.UniqueId;
        }
        private WhoAmIModel WhoAmI()
        {
            Send("whoami");
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
            while(ClientUniqueIdFromClidReceived.WaitOne(Timeout))
            {
                GetUidFromClidModel re;
                if((re = UidFromClidResponses.FirstOrDefault(x => x.ClientId == clid)) != null)
                {
                    UidFromClidResponses.Remove(re);
                    return re.ClientUniqueId;
                }
            }
            throw new UserNotFoundException(new UserNotFoundEventArgs() { ClientId = clid });
        }
        private string GetUniqueId(ClientModel client) => GetUniqueId(client.ClientId);
        private void MoveClient(IUser user, ChannelModel targetChannel)
        {
            Send(string.Format("clientmove clid={0} cid={1}", user.ClientId, targetChannel.ChannelId));
        }
        private void PokeClient(IUser user, string message)
        {
            Send(string.Format("clientpoke clid={0} msg={1}", user.ClientId, message.Replace(" ", "\\s")));
        }
        private ClientModel GetClient(string name)
        {
            ClientModel m;
            if ((m = ClientList.FirstOrDefault(x => x.ClientName.ToLower() == name.ToLower())) != null)
            {
                m.UniqueId = GetUniqueId(m);
                return m;
            }
            else
            {
                UpdateClientList();
                if ((m = ClientList.FirstOrDefault(x => x.ClientName.ToLower() == name.ToLower())) != null)
                {
                    m.UniqueId = GetUniqueId(m);
                    return m;
                }
                else throw new UserNotFoundException(new UserNotFoundEventArgs() { ClientName = name });
            }
        }
        private ClientModel GetClient(int clid)
        {
            ClientModel m;
            if ((m = ClientList.FirstOrDefault(x => x.ClientId == clid)) != null)
            {
                m.UniqueId = GetUniqueId(m);
                return m;
            }
            else
            {
                UpdateClientList();
                if ((m = ClientList.FirstOrDefault(x => x.ClientId == clid)) != null)
                {
                    m.UniqueId = GetUniqueId(m);
                    return m;
                }
                else throw new UserNotFoundException(new UserNotFoundEventArgs() { ClientId = clid });
            }
        }
        private DetailedClientModel GetDetailedClient(ClientModel model)
        {
            Send(string.Format("clientinfo clid={0}", model.ClientId));
            DetailedClientModel re;
            while (DetailedClientReceived.WaitOne(Timeout))
                if ((re = DetailedClientResponses.FirstOrDefault(x => x.ClientName == model.ClientName)) != null)
                {
                    lock (DetailedClientResponses)
                        DetailedClientResponses.Remove(re);
                    return re;
                }
            throw new UserNotFoundException(new UserNotFoundEventArgs() { ClientName = model.ClientName, ClientId = model.ClientId });
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
                Console.WriteLine("Failed to update clientlist.");
        }
        private void Send(string message)
        {
            Task.Run(() => connection.Send(Encoding.ASCII.GetBytes(message + "\n\r")));
        }
        private void SendTextMessage(string message)
        {
            Send(string.Format(@"sendtextmessage targetmode=2 msg=\n{0}", message.Replace(" ", "\\s")));
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
            string[] msgs = msg.Split(new string[] { "\n\r" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < msgs.Length; i++)
            {
                HandleReply(msgs[i]);
            }
        }
        private void HandleReply(string line)
        {
            Match m;
            if ((m = RegPatterns.ErrorLine.Match(line)).Success)
            {
                HandleErrorMessage(new ErrorModel(m));
            }
            else if ((m = RegPatterns.TextMessage.Match(line)).Success)
            {
                HandleMessage(new MessageModel(m));
            }
            else if ((m = RegPatterns.ClientMoved.Match(line)).Success)
            {
                HandleClientMoved(new ClientMovedModel(m));
            }
            else if((m = RegPatterns.ClientMovedByAdmin.Match(line)).Success)
            {
                HandleClientMoved(new ClientMovedModel(m));
            }
            else if ((m = RegPatterns.ClientLeftView.Match(line)).Success)
            {
                HandleClientLeftView(new ClientLeftViewModel(m));
            }
            else if ((m = RegPatterns.ClientEnteredView.Match(line)).Success)
            {
                HandleClientEnterView(new ClientEnteredViewModel(m));
            }
            else if ((m = RegPatterns.ClientUniqueIdFromId.Match(line)).Success)
            {
                UidFromClidResponses.Add(new GetUidFromClidModel(m));
                ClientUniqueIdFromClidReceived.Set();
            }
            else if ((m = RegPatterns.Client.Match(line)).Success)
            {
                List<ClientModel> ch = new List<ClientModel>();
                ch.Add(new ClientModel(m));
                var chs = line.Split('|');
                for (int i = 1; i < chs.Length; i++)
                    ch.Add(new ClientModel(RegPatterns.Client.Match(chs[i])));
                ClientList = ch;
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
            else if ((m = RegPatterns.DetailedClient.Match(line)).Success)
            {
                DetailedClientResponses.Add(new DetailedClientModel(m));
                DetailedClientReceived.Set();
            }
            else if ((m = RegPatterns.WhoAmI.Match(line)).Success)
            {
                Me = new WhoAmIModel(m);
                WhoAmIReceived.Set();
            } else if ((m = RegPatterns.ChannelDeleted.Match(line)).Success)
            {
                HandleChannelDeleted(new ChannelDeletedModel(m));
            }
        }
        private void HandleErrorMessage(ErrorModel model)
        {
            if (model.Id != 0)
                Console.WriteLine(model.Line);
            ErrorLineReceived.Set();
        }
        private void HandleChannelDeleted(ChannelDeletedModel model)
        {
            Dispose();
        }
        private void HandleClientEnterView(ClientEnteredViewModel model)
        {
            try
            {
                ClientModel client = GetClient(model.ClientId);
                client.ChannelId = model.ToChannelId;
                ClientJoined(client);
            } catch (UserNotFoundException) { }
        }
        private void HandleClientLeftView(ClientLeftViewModel model)
        {
            ClientModel re;
            if((re = ClientList.FirstOrDefault(x => x.ClientId == model.ClientId)) != null)
            {
                re.ChannelId = 0;
                ClientLeft(re);
                ClientList.RemoveAll(x => x.ClientId == model.ClientId);
            }
        }
        private void HandleClientMoved(ClientMovedModel model)
        {
            try
            {
                ClientModel client = GetClient(model.ClientId);
                client.ChannelId = model.ChannelToId;
                if (model.ChannelToId != ThisChannel.ChannelId)
                    ClientLeft(client);
                else
                    ClientJoined(client);
            }
            catch (UserNotFoundException) { }
        }
        private void ClientJoined(ClientModel client)
        {
            if (!config.AllowedInChannel(client.UniqueId))
            {
                PokeClient(client, "You are not allowed in this channel.");
                Kick(client);
            }
        }
        private void ClientLeft(ClientModel client)
        {
            if (Owner.UniqueId == client.UniqueId)
            {
                Reset();
                SendTextMessage("This channel is now unclaimed. To claim possession type !claim.");
            }
        }
        private void DisplayHelp()
        {
            SendTextMessage("I am a Teamspeakbot here to control the server. Type !claim to claim possession of this channel. !cmdlist displays a list of commands. If you have any feedback or thoughts you can type !feedback followed by your message and Jakkes will see it.");
        }
        private void Kick(ClientModel client)
        {

            if (GetDetailedClient(client).ChannelId == ThisChannel.ChannelId)
                MoveClient(client, DefaultChannel);
            else SendTextMessage(client.ClientName + " is not in this channel.");
        }
        private void DisplayBanlist()
        {
            SendTextMessage("Banlist:\n" + string.Join("\n", config.Banlist));
        }
        private void BanlistAdd(string name)
        {
            try
            {
                var client = GetClient(name);
                config.Ban(client.UniqueId);
                Kick(client);
                PokeClient(client, "You were banned from this channel.");
                SendTextMessage(name + " is now banned from this channel.");
            }
            catch (UserNotFoundException) { SendTextMessage("Could not find user " + name); }
            catch (UserNotInChannelException) { }
        }
        private void BanlistRemove(string name)
        {
            try
            {
                config.Unban(GetClient(name).UniqueId);
            } catch (UserNotFoundException) { SendTextMessage("Could not find user " + name); }
        }
        private void HandleMessage(MessageModel model)
        {
            if (model.Words[0].StartsWith("!"))
            {
                if (model.Words[0] == "!help")
                    DisplayHelp();
                else if (model.Words[0] == "!feedback" && model.Words.Length > 1)
                    Console.WriteLine(string.Join(" ", model.Words, 1, model.Words.Length - 1));
                else if (model.Words[0] == "!cmdlist")
                    DisplayCommandList();
                else if (model.Words[0] == "!claim")
                {
                    try
                    {
                        ClaimChannel(model);
                        SendTextMessage("You are now in power of this channel.");
                    }
                    catch (Exception) { SendTextMessage("This channel is claimed already."); }
                }
                else if (model.ClientUniqueId == Owner.UniqueId)
                {

                    if (model.Words[0] == "!kick")
                    {
                        try { Kick(GetClient(string.Join(" ", model.Words, 1, model.Words.Length - 1))); }
                        catch (UserNotFoundException ex) { SendTextMessage("Could not find user " + ex.ClientName); }
                    }
                    else if (model.Words[0] == "!banlist")
                    {
                        if (model.Words.Length == 1)
                            DisplayBanlist();
                        else if (model.Words.Length > 2)
                        {
                            if (model.Words[1] == "add")
                                try
                                {
                                    string name = string.Join(" ", model.Words, 2, model.Words.Length - 2);
                                    BanlistAdd(name);
                                }
                                catch (UserNotFoundException ex)
                                {
                                    SendTextMessage("Could not find user " + ex.ClientName);
                                }
                            else if (model.Words[1] == "remove")
                            {
                                try
                                {
                                    string name = string.Join(" ", model.Words, 2, model.Words.Length - 2);
                                    BanlistRemove(name);
                                    SendTextMessage(name + " is now unbanned.");
                                }
                                catch (UserNotFoundException ex)
                                {
                                    SendTextMessage("Could not find user " + ex.ClientName);
                                }
                            }
                        }
                    }
                    else if (model.Words[0] == "!whitelist")
                    {
                        if (model.Words.Length == 1)
                            DisplayWhiteList();
                        else if (model.Words.Length == 2)
                        {
                            if (model.Words[1] == "on")
                            {
                                ActiveWhitelist();
                                SendTextMessage("This channel is now in whitelist mode");
                            }
                            else if (model.Words[1] == "off")
                            {
                                DeactiveWhitelist();
                                SendTextMessage("This channel is now in banlist mode.");
                            }
                        }
                        else
                        {
                            if (model.Words[1] == "add")
                            {
                                try
                                {
                                    string name = string.Join(" ", model.Words, 2, model.Words.Length - 2);
                                    WhitelistAdd(name);
                                    SendTextMessage(name + " is now on the whitelist.");
                                }
                                catch (UserNotFoundException ex)
                                {
                                    SendTextMessage("Could not find user " + ex.ClientName);
                                }
                            }
                            else if (model.Words[1] == "remove")
                                try
                                {
                                    string name = string.Join(" ", model.Words, 2, model.Words.Length - 2);
                                    WhitelistRemove(name);
                                    SendTextMessage(name + " is now removed from the whitelist.");
                                }
                                catch (UserNotFoundException ex)
                                {
                                    SendTextMessage("Could not find user " + ex.ClientName);
                                }
                        }
                    }
                    else if (model.Words[0] == "!transfer")
                    {
                        if (model.Words.Length > 1)
                        {
                            try
                            {
                                string name = string.Join(" ", model.Words, 1, model.Words.Length - 1);
                                TransferOwnership(name);
                            }
                            catch (UserNotInChannelException ex) { SendTextMessage(ex.ClientName + " is not in this channel."); }
                            catch (UserNotFoundException ex) { SendTextMessage("Could not find user " + ex.ClientName); }
                        }
                    }
                }
            }
        }
        private void ClaimChannel(MessageModel model)
        {
            if (Owner == null)
            {
                try
                {
                    ClientModel m = GetClient(model.ClientId);
                    Owner = m;
                    SetChannelName(string.Format("{0} ({1})", RealChannelName, m.ClientName));
                }
                catch (UserNotFoundException) { SendTextMessage("Something went wrong..."); }
            }
            else throw new Exception("Channel is claimed already");
        }
        private void SetChannelName(string name)
        {
            Send(string.Format("channeledit cid={0} channel_name={1}", ThisChannel.ChannelId, name.Replace(" ","\\s")));
            ErrorLineReceived.WaitOne(Timeout);
        }
        private void TransferOwnership(string name)
        {
            try
            {
                var Client = GetClient(name);
                var DetailedClient = GetDetailedClient(Client);
                if (DetailedClient.ChannelId == ThisChannel.ChannelId)
                {
                    Owner = Client;
                    PokeClient(Client, "You are now the owner of this channel.");
                    SetChannelName(string.Format("{0} ({1})", RealChannelName, name));
                    SendTextMessage("Transfered ownership to " + name);
                }
                else throw new UserNotInChannelException(new UserNotInChannelEventArgs() { ClientName = Client.ClientName, ClientId = Client.ClientId });
            } catch (UserNotFoundException ex) { SendTextMessage("Could not find user " + ex.ClientName); }
        }
        private void WhitelistAdd(string name)
        {
            try
            {
                config.AddToWhitelist(GetClient(name).UniqueId);
            } catch (UserNotFoundException) { SendTextMessage("Could not find user " + name); }
        }
        private void WhitelistRemove(string name)
        {
            try
            {
                config.RemoveFromWhitelist(GetClient(name).UniqueId);
            } catch (UserNotFoundException) { SendTextMessage("Could not find user " + name); }
        }
        private void DeactiveWhitelist() => config.UseBanlist();
        private void ActiveWhitelist() => config.UseWhitelist();
        private void DisplayWhiteList()
        {
            SendTextMessage("Whitelist:\n" + string.Join("\n", config.Whitelist));
        }
        private void DisplayCommandList()
        {
            SendTextMessage(@"Command list:\n!cmdlist - Displays this message.\n!help - Displays information about me.\n!feedback <message> - Sends feedback to Jakkes. Example: !feedback This bot is amazing!\n!claim - Claims the channel you are currently in.\n!kick <user> - Kicks a user from the channel. Example: !kick Jakkes\n!banlist - Displays the banlist.\n!banlist add <user> - Bans a user from the channel. Example: !banlist add Jakkes\n!banlist remove <user> - Unbans a user from the channel. Example: !banlist remove Jakkes\n!whitelist - Displays the whitelist.\n!whitelist on - Activates the whitelist.\n!whitelist off - Deactivates the whitelist.\n!whitelist add <user> - Adds a user to the whitelist. Example: !whitelist add Jakkes\n!whitelist remove <user> - Removes a user from the whitelist. Exmaple: !whitelist remove Jakkes\n!transfer <user> - Transfers the ownership to another user. Example: !transfer Jakkes");
        }
        public void Dispose()
        {
            SetChannelName(RealChannelName);
            readTimer.Dispose();
            loopTimer.Dispose();
            connection.Dispose();
            Disposed?.Invoke(this, new EventArgs());
        }
    }
}
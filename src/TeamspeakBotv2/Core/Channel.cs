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
using TeamspeakBotv2.Commands;

namespace TeamspeakBotv2.Core
{
    public class Channel : IDisposable
    {

        private Queue<Command> _responseQueue = new Queue<Command>();

        public event EventHandler Disposed;
        public bool Active
        {
            get
            {
                try
                {
                    return connection.Connected && WhoAmI().ChannelId == ThisChannel.ChannelId;
                } catch (Exception) { return false; }
            }
        }
        public bool Connected
        {
            get { return connection.Connected; }
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
        private int Bantime;

        private Socket connection;

        private ClientModel Owner = null;
        private Config config = new Config();
        private Queue<ClientModel> OwnerQueue = new Queue<ClientModel>();
        
        private WhoAmIModel Me;
        private ChannelModel[] ChannelList;
        private List<ClientModel> ClientList = new List<ClientModel>();
        private List<GetUidFromClidModel> UidFromClidResponses = new List<GetUidFromClidModel>();
        private Dictionary<string, DetailedClientModel> DetailedClientResponses = new Dictionary<string, DetailedClientModel>();

        private Dictionary<string, int> SpamCount = new Dictionary<string, int>();
        private Timer _spamTimer;

        private Timer readTimer;
        private Timer loopTimer;

        public Channel(string channel, string parent, string defaultchannel, IPEndPoint host, string username, string password, int serverId, int timeout, int banTime)
        {
            Bantime = banTime;
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
            init(username,password,defaultchannel,channel,parent,serverId);
            loopTimer = new Timer(new TimerCallback((object state) =>
            {
                try
                {
                    if (Owner != null)
                    {
                        var m = _getClient(Owner.ClientId);
                        if (m.ChannelId != ThisChannel.ChannelId)
                            throw new Exception();
                    }
                }
                catch (Exception) { Reset(); Console.WriteLine("Had to reset."); }
            }), null, 5000, 300000);
            _spamTimer = new Timer(new TimerCallback((o) =>
            {
                var keys = SpamCount.Keys.ToArray();
                foreach (var key in keys)
                {
                    if (SpamCount.ContainsKey(key))
                    {
                        SpamCount[key]--;
                        if (SpamCount[key] == 0)
                            SpamCount.Remove(key);
                    }
                }
            }), null, 5000, 10000);
            Console.WriteLine("Starting bot in " + channel);
        }

        /// <summary>
        /// Logs into the query
        /// </summary>
        /// <param name="username">Query username</param>
        /// <param name="password">Query password</param>
        internal void _login(string username, string password)
        {
            var cmd = new LoginCommand(username, password);
            Send(cmd);
            if(!cmd.Succeeded(Timeout))
                throw new LoginException();
        }
        /// <summary>
        /// Selects server
        /// </summary>
        /// <param name="id">Server id</param>
        internal void _selectServer(int id)
        {
            var cmd = new SelectServerCommand(id);
            Send(cmd);
            if(cmd.Succeeded(Timeout))
                throw new SelectServerException();
        }
        /// <summary>
        /// Creates and moves the bot to a temporary channel.
        /// </summary>
        /// <param name="channelName">Channel name</param>
        /// <param name="parent">Parent channel</param>
        /// <exception cref="CreateChannelException">If failed.</exception>
        internal void _createChannel(string channelName, ChannelModel parent)
        {
            _createChannel(channelName, parent.ChannelId);
        }
        /// <summary>
        /// Creates and moves the bot to a temporary channel.
        /// </summary>
        /// <param name="channelName">Channel name</param>
        /// <param name="parentId">ID of parent channel</param>
        /// <exception cref="CreateChannelException">If failed.</exception>
        internal void _createChannel(string channelName, int parentId)
        {
            var cmd = new CreateChannelCommand(channelName, parentId);
            Send(cmd);
            if(cmd.Succeeded(Timeout))
                throw new CreateChannelException(channelName);
        }
        /// <summary>
        /// Gets information of a channel.
        /// </summary>
        /// <param name="channelName">Name of channel</param>
        /// <returns>The channel</returns>
        internal ChannelModel _getChannel(string channelName)
        {
            var cmd = new GetChannelCommand(channelName);
            if(SendSuccessfully(cmd,Timeout))
                return cmd.Result;
            else
                throw new GetChannelException(channelName);
        }
        
        
        
        /// <summary>
        /// Logs onto the server and initilizes the bot.
        /// </summary>
        /// <param name="username">Query username</param>
        /// <param name="password">Query password</param>
        /// <param name="defaultChannel">Password to kick people into.</param>
        /// <param name="channel">Channel name</param>
        /// <param name="parent">Parent channel name</param>
        /// <param name="serverId">Server id</param>
        private void init(string username, string password, string defaultChannel, string channel, string parent, int serverId)
        {
            try
            {
                _login(username, password);
                _selectServer(serverId);
                _createChannel(channel, _getChannel(parent));
                ThisChannel = _getChannel(channel);
                DefaultChannel = _getChannel(defaultChannel);
                RealChannelName = channel;
                _registerToEvents();
                
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Dispose();
            }
        }
        private void _registerToEvents()
        {
            if(!SendSuccessfully(new RegisterToEventCommand(Event.Channel,ThisChannel.ChannelId),Timeout)){
                throw new RegisterToEventException(Event.Channel);
            }
            if(!!SendSuccessfully(new RegisterToEventCommand(Event.TextChannel,ThisChannel.ChannelId),Timeout)){
                throw new RegisterToEventException(Event.TextChannel);
            }
        }
        private void Reset()
        {
            try
            {
                config.Save(Owner.UniqueId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to save config.");
                Console.WriteLine(ex.Message);
            }

            SetChannelName(RealChannelName);
            OwnerQueue.Clear();
            Owner = null;
            config = new Config();
        }
        private bool isOwner(ClientModel client)
        {
            return isOwner(client.UniqueId);
        }
        private bool isOwner(string UniqueId)
        {
            return Owner != null && Owner.UniqueId == UniqueId;
        }
        private string _getUID(int clid)
        {
            var cmd = new GetUIDCommand(clid);
            if(SendSuccessfully(cmd, Timeout)){
                return cmd.Result;
            } else 
                throw new GetUIDException(cmd.ErrorMessage);
        }
        private string _getUID(ClientModel client) => _getUID(client.ClientId);
        private void _moveClient(IUser user, ChannelModel targetChannel)
        {
            if(!SendSuccessfully(new MoveClientCommand(user, targetChannel), Timeout)){
                throw new MoveClientException();
            }
        }
        private void _pokeClient(int clid, string message)
        {
            if(!SendSuccessfully(new PokeClientCommand(clid,message),Timeout))
                throw new PokeClientException();
        }
        private void _pokeClient(IUser user, string msg) => _pokeClient(user.ClientId, msg);
        private ClientModel _getClient(string name)
        {
            var cmd = new GetClientCommand(name);
            if(!SendSuccessfully(cmd, Timeout))
                throw new GetClientException();
            else
                return cmd.Result;
        }
        /// <summary>
        /// Retrieves client information.
        /// </summary>
        /// <param name="clid">Client id</param>
        /// <returns>Client data</returns>
        /// <exception cref="GetClientException"></exception>
        private ClientModel _getClient(int clid)
        {
            var cmd = new GetClientCommand(clid);
            if(!SendSuccessfully(cmd, Timeout))
                throw new GetClientException();
            else
                return cmd.Result;
        }
        private DetailedClientModel _getDetailedClient(ClientModel model)
        {
            return _getDetailedClient(model.ClientId);
        }
        private DetailedClientModel _getDetailedClient(int clid)
        {
            var cmd = new GetDetailedClientCommand(clid);
            if (SendSuccessfully(cmd, Timeout))
                return cmd.Result;
            else
                throw new GetDetailedClientException(cmd.ErrorMessage);
        }
        private void _ban(int clid, int time)
        {
            if (!SendSuccessfully(new BanCommand(clid, time), Timeout))
                throw new BanException();
        }
        private void _ban(ClientModel client, int time)
        {
            _ban(client.ClientId, time);
        }
        /// <summary>
        /// Sends a command to the server and then places it in the response queue.
        /// </summary>
        /// <param name="cmd">Command to be sent.</param>
        private void Send(Command cmd)
        {
            _send(cmd.Message);
            _responseQueue.Enqueue(cmd);
        }
        /// <summary>
        /// Sends a command to the query and waits for the result.
        /// </summary>
        /// <param name="cmd">Command to be sent</param>
        /// <param name="timeout">Timeout</param>
        /// <returns>True if successful, false if an error was encountered.</returns>
        private bool SendSuccessfully(Command cmd, int timeout){
            Send(cmd);
            return cmd.Succeeded(timeout);
        }
        internal void _send(string message)
        {
            Task.Run(() => { connection.Send(Encoding.ASCII.GetBytes(message + "\n\r")); });
        }
        private void _sendTextMessage(string message)
        {
            if (!SendSuccessfully(new SendTextCommand(message), Timeout))
                throw new SendTextCommandException();
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
            var errormatch = RegPatterns.ErrorLine.Match(line);
            if (errormatch.Success)
            {
                try { _responseQueue.Dequeue().HandleErrorLine(new ErrorModel(errormatch)); }
                catch (InvalidOperationException) { }
            } else
            {
                // Check if the message is a response message.
                try { _responseQueue.Peek()?.HandleResponse(line); }
                catch (RegexMatchException)
                {
                    // Message is not a response.
                    var m = RegPatterns.ClientMoved.Match(line);
                    if ((m = RegPatterns.TextMessage.Match(line)).Success)
                    {
                        HandleMessage(new MessageModel(m));
                    }
                    else if ((m = RegPatterns.ClientMoved.Match(line)).Success)
                    {
                        HandleClientMoved(new ClientMovedModel(m));
                    }
                    else if ((m = RegPatterns.ClientMovedByAdmin.Match(line)).Success)
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

                }
                catch (Exception ex) { Console.WriteLine(ex.Message; }
            }
        }
        private void HandleClientEnterView(ClientEnteredViewModel model)
        {
            try
            {
                ClientModel client = _getClient(model.ClientId);
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
            if(model.ChannelToId == ThisChannel.ChannelId)
                try { ClientJoined(_getClient(model.ClientId)); }
                catch (GetClientException ex) { Console.WriteLine(ex); }
            try
            {
                ClientModel client = _getClient(model.ClientId);
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
            if (client.UniqueId == "serveradmin")
                return;
            if (!config.AllowedInChannel(client.UniqueId))
            {
                _pokeClient(client, "You are not allowed in this channel. Do not spam, it'll get you banned.");

                if (SpamCount.ContainsKey(client.UniqueId))
                {
                    SpamCount[client.UniqueId]++;
                    if (SpamCount[client.UniqueId] >= 10)
                    {
                        try { BanForSpam(client); }
                        catch (BanException ex) { Console.WriteLine(ex.Message); }
                        return;
                    }
                }
                else
                    SpamCount.Add(client.UniqueId, 1);

                try
                {
                    Kick(client);
                } catch (UserNotInChannelException) { }
            } else if(Owner == null)
            {
                SetOwner(client);
            } else
            {
                OwnerQueue.Enqueue(client);
            }
        }
        private void BanForSpam(ClientModel client)
        {
            try
            {
                _ban(client, Bantime);
            } catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
        private void ClientLeft(ClientModel client)
        {
            if (isOwner(client))
            {
                while(OwnerQueue.Count > 0)
                {
                    try
                    {
                        var temp = OwnerQueue.Dequeue();
                        if (_getDetailedClient(temp).ChannelId == ThisChannel.ChannelId) {
                            SetOwner(temp);
                            return;
                        }
                    }
                    catch (Exception) { }
                }
                Reset();
            }
        }
        private void DisplayHelp()
        {
            _sendTextMessage(@"I am a TeamspeakBot here to control the server. !cmdlist displays a list of commands. If you have any feedback or thoughts you can type !feedback followed by your message and I will see it.\n\nNew feature! People spamjoining will be automatically banned from the server.");
        }
        private void Kick(ClientModel client)
        {
            if (_getDetailedClient(client).ChannelId == ThisChannel.ChannelId)
            {
                _moveClient(client, DefaultChannel);
            }
            else throw new UserNotInChannelException(new UserNotInChannelEventArgs() { ClientId = client.ClientId, ClientName = client.ClientName });
        }
        private void DisplayBanlist()
        {
            _sendTextMessage("Banlist:\\n" + string.Join("\\n", config.Banlist.Values));
        }
        private void BanlistAdd(string name)
        {
            try
            {
                var client = _getClient(name);
                config.Ban(client.UniqueId,name);
                _sendTextMessage(name + " is now banned from this channel.");
                Kick(client);
                _pokeClient(client, "You were banned from this channel.");
            }
            catch (UserNotFoundException) { _sendTextMessage("Could not find user " + name); }
            catch (UserNotInChannelException) { }
            catch (ArgumentException) { _sendTextMessage(name + " is already on the banlist."); }
        }
        private void BanlistRemove(string name)
        {
            try
            {
                config.Unban(_getClient(name).UniqueId);
            } catch (UserNotFoundException) { _sendTextMessage("Could not find user " + name); }
        }
        private void HandleMessage(MessageModel model)
        {
            try
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
                            _sendTextMessage("You are now in power of this channel.");
                        }
                        catch (Exception) { _sendTextMessage("This channel is claimed already."); }
                    }
                    else if (isOwner(model.ClientUniqueId))
                    {

                        if (model.Words[0] == "!kick")
                        {
                            try { Kick(_getClient(string.Join(" ", model.Words, 1, model.Words.Length - 1))); }
                            catch (UserNotFoundException ex) { _sendTextMessage("Could not find user " + ex.ClientName); }
                            catch (UserNotInChannelException ex) { _sendTextMessage(ex.ClientName + " is not in this channel."); }
                        }
                        else if (model.Words[0] == "!banlist")
                        {
                            if (model.Words.Length == 1)
                                DisplayBanlist();
                            else if(model.Words.Length == 2)
                            {
                                if(model.Words[1] == "clear")
                                {
                                    config.ClearBanlist();
                                    _sendTextMessage("Banlist is now empty.");
                                }
                            }
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
                                        _sendTextMessage("Could not find user " + ex.ClientName);
                                    }
                                else if (model.Words[1] == "remove")
                                {
                                    try
                                    {
                                        string name = string.Join(" ", model.Words, 2, model.Words.Length - 2);
                                        BanlistRemove(name);
                                        _sendTextMessage(name + " is now unbanned.");
                                    }
                                    catch (UserNotFoundException ex)
                                    {
                                        _sendTextMessage("Could not find user " + ex.ClientName);
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
                                    _sendTextMessage("This channel is now in whitelist mode");
                                }
                                else if (model.Words[1] == "off")
                                {
                                    DeactiveWhitelist();
                                    _sendTextMessage("This channel is now in banlist mode.");
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
                                    }
                                    catch (UserNotFoundException ex)
                                    {
                                        _sendTextMessage("Could not find user " + ex.ClientName);
                                    }
                                }
                                else if (model.Words[1] == "remove")
                                    try
                                    {
                                        string name = string.Join(" ", model.Words, 2, model.Words.Length - 2);
                                        WhitelistRemove(name);
                                        _sendTextMessage(name + " is now removed from the whitelist.");
                                    }
                                    catch (UserNotFoundException ex)
                                    {
                                        _sendTextMessage("Could not find user " + ex.ClientName);
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
                                catch (UserNotInChannelException ex) { _sendTextMessage(ex.ClientName + " is not in this channel."); }
                                catch (UserNotFoundException ex) { _sendTextMessage("Could not find user " + ex.ClientName); }
                            }
                        }
                    }
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                Console.WriteLine("Error in HandleMessage. IndexOutOfRange");
                Console.WriteLine(ex.Message);
                Console.WriteLine(string.Join(" ", model.Words));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unknown error in HandleMessage.");
                Console.WriteLine(ex.Message);
            }
        }
        private void SetOwner(ClientModel client)
        {
            Owner = client;
            if (client != null)
                SetChannelName(string.Format("{0} ({1})", RealChannelName, client.ClientName));
            else
                SetChannelName(RealChannelName);
            DisplayHelp();
            try
            {
                config = Config.Load(client.UniqueId);
            } catch (Exception)
            {
                config = new Config();
            }
        }
        private void ClaimChannel(MessageModel model)
        {
            if (Owner == null)
            {
                try
                {
                    SetOwner(_getClient(model.ClientId));
                }
                catch (UserNotFoundException) { _sendTextMessage("Something went wrong..."); }
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
                var Client = _getClient(name);
                var DetailedClient = _getDetailedClient(Client);
                if (DetailedClient.ChannelId == ThisChannel.ChannelId)
                {
                    SetOwner(Client);
                    PokeClient(Client, "You are now the owner of this channel.");
                    _sendTextMessage("Transfered ownership to " + name);
                }
                else throw new UserNotInChannelException(new UserNotInChannelEventArgs() { ClientName = Client.ClientName, ClientId = Client.ClientId });
            } catch (UserNotFoundException ex) { _sendTextMessage("Could not find user " + ex.ClientName); }
        }
        private void WhitelistAdd(string name)
        {
            try
            {
                config.AddToWhitelist(_getClient(name).UniqueId,name);
                _sendTextMessage(name + " is now on the whitelist.");
            }
            catch (UserNotFoundException) { _sendTextMessage("Could not find user " + name); }
            catch (ArgumentException) { _sendTextMessage(name + " is already on the whitelist."); }
        }
        private void WhitelistRemove(string name)
        {
            try
            {
                config.RemoveFromWhitelist(_getClient(name).UniqueId);
            } catch (UserNotFoundException) { _sendTextMessage("Could not find user " + name); }
        }
        private void DeactiveWhitelist() => config.UseBanlist();
        private void ActiveWhitelist() => config.UseWhitelist();
        private void DisplayWhiteList()
        {
            _sendTextMessage("Whitelist:\\n" + string.Join("\\n", config.Whitelist.Values));
        }
        private void DisplayCommandList()
        {
            _sendTextMessage(@"Command list:\n!cmdlist - Displays this message.\n!help - Displays information about me.\n!feedback <message> - Sends feedback to Jakkes. Example: !feedback This bot is amazing!\n!claim - Claims the channel you are currently in.\n!kick <user> - Kicks a user from the channel. Example: !kick Jakkes\n!banlist - Displays the banlist.\n!banlist add <user> - Bans a user from the channel. Example: !banlist add Jakkes\n!banlist remove <user> - Unbans a user from the channel. Example: !banlist remove Jakkes\n!banlist clear - Clears the banlist.\n!whitelist - Displays the whitelist.\n!whitelist on - Activates the whitelist.\n!whitelist off - Deactivates the whitelist.\n!whitelist add <user> - Adds a user to the whitelist. Example: !whitelist add Jakkes\n!whitelist remove <user> - Removes a user from the whitelist. Exmaple: !whitelist remove Jakkes\n!transfer <user> - Transfers the ownership to another user. Example: !transfer Jakkes");
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
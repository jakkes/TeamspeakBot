using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TeamspeakBotv2.Commands;
using TeamspeakBotv2.Models;

namespace TeamspeakBotv2.Core
{
    public class Channel : IDisposable
    {

        public Command[] ResponseQueue { get { return _responseQueue.ToArray(); } }
        private List<Command> _responseQueue = new List<Command>();
        private List<Command> _sentCmds = new List<Command>();

        private List<string> sentStuff = new List<string>();

        private string infoMessage;

        public event EventHandler Disposed;
        public bool Active
        {
            get
            {
                try
                {
                    return connection.Connected && _whoAmI().ChannelId == ThisChannel.ChannelId;
                }
                catch (Exception) { return false; }
            }
        }
        public bool Connected
        {
            get { return connection.Connected; }
        }
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

        private Dictionary<string, int> SpamCount = new Dictionary<string, int>();
        private Timer _spamTimer;

        private Timer readTimer;
        private Timer loopTimer;

        public Channel(string channel, string parent, string defaultchannel, IPEndPoint host, string username, string password, int serverId, int timeout, int banTime, string infoMessage)
        {
            RealChannelName = channel;
            Bantime = banTime;
            Timeout = timeout;
            this.infoMessage = infoMessage;

            connection = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try { connection.Connect(host); }
            catch(Exception ex) { Console.WriteLine(ex.Message); Dispose(); return; }

            readTimer = new Timer(new TimerCallback(Read), null, 0, 100);

            try { init(username, password, defaultchannel, channel, parent, serverId); }
            catch(CommandException ex)
            {
                Console.WriteLine("Failed starting channel " + channel);
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.Error?.Message);
                Dispose();
                return;
            }
            
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
            Console.WriteLine("Started bot in " + channel);
        }
        /// <summary>
        /// Logs into the query
        /// </summary>
        /// <param name="username">Query username</param>
        /// <param name="password">Query password</param>
        internal void _login(string username, string password)
        {
            var cmd = new LoginCommand(username, password);
            Send(cmd, Timeout);
        }
        /// <summary>
        /// Selects server
        /// </summary>
        /// <param name="id">Server id</param>
        internal void _selectServer(int id)
        {
            var cmd = new SelectServerCommand(id);
            Send(cmd, Timeout);
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
            Send(cmd, Timeout);
        }
        /// <summary>
        /// Gets information of a channel.
        /// </summary>
        /// <param name="channelName">Name of channel</param>
        /// <returns>The channel</returns>
        internal ChannelModel _getChannel(string channelName)
        {
            var cmd = new GetChannelCommand(channelName);
            Send(cmd, Timeout);
            return (ChannelModel)cmd.Result;
        }
        internal WhoAmIModel _whoAmI()
        {
            var cmd = new WhoAmICommand();
            Send(cmd, Timeout);
            return (WhoAmIModel)cmd.Result;
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
            _login(username, password);
            _selectServer(serverId);
            _createChannel(channel, _getChannel(parent));
            ThisChannel = _getChannel(channel);
            DefaultChannel = _getChannel(defaultChannel);
            _registerToEvents();
        }
        private void _registerToEvents()
        {
            Send(new RegisterToEventCommand(Event.Channel, ThisChannel.ChannelId), Timeout);
            Send(new RegisterToEventCommand(Event.TextChannel, ThisChannel.ChannelId), Timeout);
        }
        private void Reset()
        {
            try
            {
                config.Save(Owner?.UniqueId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to save config.");
                Console.WriteLine(ex.Message);
            }
            try
            {
                SetChannelName(RealChannelName);
                OwnerQueue.Clear();
                Owner = null;
                config = new Config();
            }
            catch (Exception) { Dispose(); }
        }
        private bool isOwner(ClientModel client)
        {
            return isOwner(client.UniqueId);
        }
        private bool isOwner(string UniqueId)
        {
            return Owner != null && Owner.UniqueId == UniqueId;
        }
        private bool OwnerActive()
        {
            if(Owner != null)
            {
                try
                {
                    var t = _getDetailedClient(Owner);
                    return t.UniqueId == Owner.UniqueId && t.ChannelId == ChannelId;
                }
                catch (Exception) { return false; }
            } else
                return false;
        }
        private string _getUID(int clid)
        {
            var cmd = new GetUIDCommand(clid);
            Send(cmd, Timeout);
            return (string)cmd.Result;
        }
        private string _getUID(ClientModel client) => _getUID(client.ClientId);
        private void _moveClient(IUser user, ChannelModel targetChannel)
        {
            Send(new MoveClientCommand(user, targetChannel), Timeout);
        }
        private void _pokeClient(int clid, string message)
        {
            Send(new PokeClientCommand(clid, message), Timeout);
        }
        private void _pokeClient(IUser user, string msg) => _pokeClient(user.ClientId, msg);
        private ClientModel _getClient(string name)
        {
            var cmd = new GetClientCommand(name);
            Send(cmd, Timeout);
            ((ClientModel)(cmd.Result)).UniqueId = _getUID(((ClientModel)cmd.Result).ClientId);
            return (ClientModel)cmd.Result;
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
            Send(cmd, Timeout);
            ((ClientModel)cmd.Result).UniqueId = _getUID(clid);
            return (ClientModel)cmd.Result;
        }
        private DetailedClientModel _getDetailedClient(ClientModel model)
        {
            return _getDetailedClient(model.ClientId, model.ClientName);
        }
        private DetailedClientModel _getDetailedClient(int clid, string name)
        {
            var cmd = new GetDetailedClientCommand(clid, name);
            Send(cmd, Timeout);
            return (DetailedClientModel)cmd.Result;
        }
        private void _ban(int clid, int time)
        {
            Send(new BanCommand(clid, time), Timeout);
        }
        private void _ban(ClientModel client, int time)
        {
            _ban(client.ClientId, time);
        }
        /// <summary>
        /// Sends a command to the server and then places it in the response queue.
        /// </summary>
        /// <param name="cmd">Command to be sent.</param>
        private void Send(Command cmd, int timeout)
        {
            _responseQueue.Add(cmd);
            _send(cmd.Message);
            if (!cmd.Succeeded(timeout))
            {
                _responseQueue.Remove(cmd);
                throw new CommandException(cmd.Error, cmd.GetType());
            }
            _responseQueue.Remove(cmd);
        }
        internal void _send(string message)
        {
            connection.Send(Encoding.ASCII.GetBytes(message + "\n\r"));
        }
        private void _sendTextMessage(string message)
        {
            Send(new SendTextCommand(message), Timeout);
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
            while (connection.Available != 0)
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
            var stuff = ResponseQueue;
            var errormatch = RegPatterns.ErrorLine.Match(line);
            if (errormatch.Success)
            {
                // Message is an error line
                var errormodel = new ErrorModel(errormatch);
                for (int i = 0; i < stuff.Length; i++)
                {
                    try
                    {
                        stuff[i].HandleErrorLine(errormodel);
                        break;
                    }
                    catch (ErrorPreviouslyHandledException) { }
                }
            }
            else
            {
                // Check if the message is a response message.
                bool handled = false;

                for (int i = 0; i < stuff.Length; i++)
                {
                    try
                    {
                        stuff[i].HandleResponse(line);
                        handled = true;
                    }
                    catch (RegexMatchException) { }
                    catch (ArgumentException) { }
                }

                if (!handled)
                {
                    // Message is not a response.
                    Match m;
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
            }
        }
        private void HandleClientEnterView(ClientEnteredViewModel model)
        {
            try
            {
                ClientJoined(_getClient(model.ClientId));
            }
            catch (CommandException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.Error?.Message);
            }
        }
        private void HandleClientLeftView(ClientLeftViewModel model)
        {
            try
            {
                if (model.ChannelFromId == ThisChannel.ChannelId && !OwnerActive())
                {
                    TransferOwnershipToNext();
                }
            }
            catch (CommandException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.Error?.Message);
            }
        }
        private void HandleClientMoved(ClientMovedModel model)
        {
            if (model.ChannelToId == ThisChannel.ChannelId)
            {
                try { ClientJoined(_getClient(model.ClientId)); }
                catch (CommandException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.Error?.Message);
                }
            }
            else
            {
                try { ClientLeft(_getClient(model.ClientId)); }
                catch (CommandException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.Error?.Message);
                }
            }
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
                        catch (CommandException ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.Error?.Message);
                        }
                        return;
                    }
                }
                else
                    SpamCount.Add(client.UniqueId, 1);

                try
                {
                    Kick(client);
                }
                catch (UserNotInChannelException) { }
            }
            else if (Owner == null)
            {
                SetOwner(client);
            }
            else
            {
                OwnerQueue.Enqueue(client);
            }
        }
        private void BanForSpam(ClientModel client)
        {
            try
            {
                _ban(client, Bantime);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
        private void ClientLeft(ClientModel client)
        {
            if (isOwner(client))
                TransferOwnershipToNext();
        }
        private void TransferOwnershipToNext()
        {
            while (OwnerQueue.Count > 0)
            {
                ClientModel cl = OwnerQueue.Dequeue();
                try
                {
                    SetOwner(cl);
                    return;
                }
                catch (UserNotInChannelException) { }
                catch (Exception) { }
            }
            Reset();
        }
        private void DisplayHelp()
        {
            _sendTextMessage(infoMessage);
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
                config.Ban(client.UniqueId, name);
                _sendTextMessage(name + " is now banned from this channel.");
                Kick(client);
                _pokeClient(client, "You were banned from this channel.");
            }
            catch (CommandException ex)
            {
                if (ex.Command == typeof(GetClientCommand))
                    _sendTextMessage("Could not find user " + name);
                else
                    _sendTextMessage("Something went wrong when banning...");
            }
            catch (UserNotInChannelException) { }
            catch (ArgumentException) { _sendTextMessage(name + " is already on the banlist."); }
        }
        private void BanlistRemove(string name)
        {
            try
            {
                config.Unban(_getClient(name).UniqueId);
            }
            catch (CommandException ex)
            {
                if (ex.Command == typeof(GetClientCommand))
                    _sendTextMessage("Could not find user " + name);
                else
                    _sendTextMessage("Something went wrong when unbanning...");
            }
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
                    else if (isOwner(model.ClientUniqueId))
                    {

                        if (model.Words[0] == "!kick")
                        {
                            try
                            {
                                Kick(_getClient(string.Join(" ", model.Words, 1, model.Words.Length - 1)));
                            }
                            catch (CommandException ex)
                            {
                                if (ex.Command == typeof(GetClientCommand))
                                    _sendTextMessage("Could not find user");
                                else
                                    _sendTextMessage("An unknown error was encountered.");
                            }
                            catch (UserNotInChannelException ex) { _sendTextMessage(ex.ClientName + " is not in this channel."); }
                        }
                        else if (model.Words[0] == "!banlist")
                        {
                            if (model.Words.Length == 1)
                                DisplayBanlist();
                            else if (model.Words.Length == 2)
                            {
                                if (model.Words[1] == "clear")
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
                                    catch (CommandException ex)
                                    {
                                        if (ex.Command == typeof(GetClientCommand))
                                            _sendTextMessage("Could not find user");
                                        else
                                            _sendTextMessage("An unknown error was encountered.");
                                    }
                                else if (model.Words[1] == "remove")
                                {
                                    try
                                    {
                                        string name = string.Join(" ", model.Words, 2, model.Words.Length - 2);
                                        BanlistRemove(name);
                                        _sendTextMessage(name + " is now unbanned.");
                                    }
                                    catch (CommandException ex)
                                    {
                                        if (ex.Command == typeof(GetClientCommand))
                                            _sendTextMessage("Could not find user");
                                        else
                                            _sendTextMessage("An unknown error was encountered.");
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
                                    ActivateWhiteList();
                                    _sendTextMessage("This channel is now in whitelist mode");
                                }
                                else if (model.Words[1] == "off")
                                {
                                    DeactivateWhiteList();
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
                                    catch (CommandException ex)
                                    {
                                        if (ex.Command == typeof(GetClientCommand))
                                            _sendTextMessage("Could not find user");
                                        else
                                            _sendTextMessage("An unknown error was encountered.");
                                    }
                                }
                                else if (model.Words[1] == "remove")
                                    try
                                    {
                                        string name = string.Join(" ", model.Words, 2, model.Words.Length - 2);
                                        WhitelistRemove(name);
                                        _sendTextMessage(name + " is now removed from the whitelist.");
                                    }
                                    catch (CommandException ex)
                                    {
                                        if (ex.Command == typeof(GetClientCommand))
                                            _sendTextMessage("Could not find user");
                                        else
                                            _sendTextMessage("An unknown error was encountered.");
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
                                catch (CommandException ex)
                                {
                                    if (ex.Command == typeof(GetClientCommand))
                                        _sendTextMessage("Could not find user");
                                    else
                                        _sendTextMessage("An unknown error was encountered.");
                                }
                            }
                        }
                    }
                }
            }
            catch(CommandException ex)
            {
                Console.WriteLine("Error in HandleMessage.");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.Error?.Message);
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
            if(client == null)
            {
                Reset();
                return;
            }
            if (_getDetailedClient(client).ChannelId == ChannelId)
            {
                Owner = client;
                try { SetChannelName(string.Format("{0} ({1})", RealChannelName, client.ClientName)); }
                catch (CommandException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.Error?.Message);
                }
                try { DisplayHelp(); }
                catch (CommandException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.Error?.Message);
                }
                try
                {
                    config = Config.Load(client.UniqueId);
                }
                catch (Exception)
                {
                    config = new Config();
                }
            }
            else
                throw new UserNotInChannelException(new UserNotInChannelEventArgs(client));
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
            Send(new SetChannelNameCommand(name, ThisChannel.ChannelId), Timeout);
        }
        private bool UserInChannel(ClientModel client)
        {
            return _getDetailedClient(client).ChannelId == ChannelId;
        }
        private bool UserInChannel(string name)
        {
            return _getDetailedClient(_getClient(name)).ChannelId == ChannelId;
        }
        private bool UserInChannel(int clid)
        {
            return _getDetailedClient(_getClient(clid)).ChannelId == ChannelId;
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
                    _pokeClient(Client, "You are now the owner of this channel.");
                    _sendTextMessage("Transfered ownership to " + name);
                }
                else throw new UserNotInChannelException(new UserNotInChannelEventArgs() { ClientName = Client.ClientName, ClientId = Client.ClientId });
            }
            catch (UserNotFoundException ex) { _sendTextMessage("Could not find user " + ex.ClientName); }
        }
        private void WhitelistAdd(string name)
        {
            try
            {
                config.AddToWhitelist(_getClient(name).UniqueId, name);
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
            }
            catch (UserNotFoundException) { _sendTextMessage("Could not find user " + name); }
        }
        private void DeactivateWhiteList() => config.UseBanlist();
        private void ActivateWhiteList() => config.UseWhitelist();
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
            try { readTimer?.Dispose(); } catch (Exception) { }
            try { loopTimer?.Dispose(); } catch (Exception) { }
            try { connection?.Dispose(); } catch (Exception) { }
            
            Disposed?.Invoke(this, new EventArgs());
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamspeakBotv2.Core
{
    public class UserNotFoundException : Exception
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; }

        public UserNotFoundException(UserNotFoundEventArgs args)
        {
            ClientId = args.ClientId;
            ClientName = args.ClientName;
        }
    }
    public class UserNotFoundEventArgs
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; }
    }

    public class UserNotInChannelException : Exception
    {
        public UserNotInChannelEventArgs args { get; set; }
        public string ClientName { get { return args.ClientName; } }
        public int ClientId { get { return args.ClientId; } }

        public UserNotInChannelException(UserNotInChannelEventArgs args)
        {
            this.args = args;
        }
        public UserNotInChannelException(string username, int id)
        {
            args = new UserNotInChannelEventArgs() { ClientName = username, ClientId = id };
        }
    }

    public class UserNotInChannelEventArgs
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; }
    }

    public class ChannelNotFoundException : Exception
    {
        public string ChannelName { get; set; }
    }

    public class CreateChannelException : Exception
    {
        public string ChannelName { get; set; }
        public CreateChannelException(string channel) : base("Failed to create channel " + channel){
            ChannelName = channel;
        }
    }

    public class FailedToRegisterEventsEventArgs
    {
        public string Event { get; set; }
        public FailedToRegisterEventsEventArgs(string ev){
            Event = ev;
        }
        public FailedToRegisterEventsEventArgs(){}
    }

    public class FailedToRegisterEventsException : Exception
    {
        public FailedToRegisterEventsEventArgs Args { get; set; }
        public FailedToRegisterEventsException(FailedToRegisterEventsEventArgs args) : this()
        {
            this.Args = args;
        }
        public FailedToRegisterEventsException(string ev) : this(){
            Args = new FailedToRegisterEventsEventArgs(ev);
        }
        public FailedToRegisterEventsException() : base("Failed to register to events"){}
    }

    public class LoginException : Exception
    {
        public LoginException(string msg) : base(msg)
        {
        }
        public LoginException() : base("Failed to login.")
        {
        }
    }

    public class SelectServerException : Exception
    {
        public SelectServerException() : base("Failed to select server") { }
    }
}
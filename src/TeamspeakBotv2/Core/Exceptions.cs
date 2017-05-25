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
        public string ClientName { get; set; }
        public int ClientId { get; set; }

        public UserNotInChannelException(UserNotInChannelEventArgs args)
        {
            ClientName = args.ClientName;
            ClientId = args.ClientId;
        }
    }

    public class UserNotInChannelEventArgs
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; }
    }

    public class ChannelNotFoundException : Exception{
        public string ChannelName { get; set; }
    }

    public class FailedToCreateChannelException : Exception {
        public string ChannelName { get; set; }
    }

    public class FailedToRegisterEventsException : Exception{
        public FailedToRegisterEventsEventArgs Args { get; set; }
        public FailedToRegisterEventsException(string event) {
            Args = new FailedToRegisterEventsEventArgs(){Event = event};
        }
    }

    public class FailedToRegisterEventsEventArgs{
        public string Event { get; set; }
    }
}

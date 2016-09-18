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
}

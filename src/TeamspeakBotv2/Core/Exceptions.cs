using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamspeakBotv2.Commands;

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

    
    
    

    
}
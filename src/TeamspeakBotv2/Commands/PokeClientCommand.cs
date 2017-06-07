using System;
using TeamspeakBotv2.Models;
using TeamspeakBotv2.Core;

namespace TeamspeakBotv2.Commands
{
    public class PokeClientCommand : Command
    {
        public PokeClientCommand(IUser user, string message)
            : this(user.ClientId, message) {
        }
        public PokeClientCommand(int clid, string msg){
            Message = string.Format("clientpoke clid={0} msg={1}", clid, msg.Replace(" ", "\\s"));
        }
        public override void HandleResponse(string msg)
        {
            
        }
    }

    public class PokeClientException : Exception{
        public PokeClientException() : base("Failed to poke client"){

        }
    }
}
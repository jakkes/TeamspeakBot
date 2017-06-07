using System;
using TeamspeakBotv2.Core;
using TeamspeakBotv2.Models;

namespace TeamspeakBotv2.Commands
{
    public class MoveClientCommand : Command
    {
        public MoveClientCommand(IUser user, ChannelModel targetChannel)
            : this(user.ClientId, targetChannel.ChannelId) {
        }
        public MoveClientCommand(int clid, int cid){
            Message = string.Format("clientmove clid={0} cid={1}", clid, cid);
        }
        public override void HandleResponse(string msg)
        {
            
        }
    }

    public class MoveClientException : Exception {
        public MoveClientException(string msg) : base(msg){

        }
        public MoveClientException() : base("Failed to move client."){
            
        }
    }
}
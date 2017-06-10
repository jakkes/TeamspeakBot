using System;
using TeamspeakBotv2.Core;
using TeamspeakBotv2.Models;

namespace TeamspeakBotv2.Commands {
    public class SetChannelNameCommand : Command {
        public SetChannelNameCommand (string name, int cid) {
            Message = string.Format("channeledit cid={0} channel_name={1}", cid, name.Replace (" ", "\\s"));
        }
        public override void HandleResponse (string msg) {
            
        }
    }
}
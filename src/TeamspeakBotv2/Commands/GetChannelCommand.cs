using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamspeakBotv2.Models;
using TeamspeakBotv2.Core;

namespace TeamspeakBotv2.Commands
{
    public class GetChannelCommand : CollectCommand
    {
        private string _name;
        private int _cid = -1;
        public override void HandleResponse(string msg)
        {
            var lines = msg.Split('|');
            for(int i = 0; i < lines.Length; i++){
                var m = RegPatterns.Channel.Match(lines[i]);
                if(m.Success){
                    var mo = new ChannelModel(m);
                    if((_cid != -1 && mo.ChannelId == _cid) || (!string.IsNullOrEmpty(_name) && mo.ChannelName == _name)){
                        Result = mo;
                        break;
                    }
                } else
                {
                    throw new RegexMatchException(msg, RegPatterns.Channel);
                }
            }
            if (Result == null)
            {
                _failed("Could not find channel.");
                return;
            }

            base.HandleResponse(msg);
        }
        public GetChannelCommand(string channelName) : this() {
            _name = channelName;
        }
        public GetChannelCommand(int cid) : this() {
            _cid = cid;
        }
        private GetChannelCommand(){
            Message = "channellist";
        }
    }
}

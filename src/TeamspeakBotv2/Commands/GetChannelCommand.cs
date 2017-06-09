using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamspeakBotv2.Models;
using TeamspeakBotv2.Core;

namespace TeamspeakBotv2.Commands
{
    public class GetChannelCommand : Command
    {
        private string _name;
        private int _cid = -1;
        public ChannelModel Result { get; private set; }
        public override void HandleResponse(string msg)
        {
            var lines = msg.Split('|');
            for(int i = 0; i < lines.Length; i++){
                var m = RegPatterns.Channel.Match(lines[i]);
                if(m.Success){
                    var mo = new ChannelModel(m);
                    if((_cid != -1 && mo.ChannelId == _cid) || (!string.IsNullOrEmpty(_name) && mo.ChannelName == _name)){
                        Result = mo;
                        return;
                    }
                } else
                {
                    throw new RegexMatchException();
                }
            }
            _failed("Could not find channel.");
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

    public class GetChannelException : Exception{
        public string ChannelName { get; set; }
        public int? ChannelId { get; set; }
        public GetChannelException(string channelName) : base("Could not find channel " + channelName){
            ChannelName = channelName;
        }
        public GetChannelException(int cid) : base("Could not find channel id " + cid){
            ChannelId = cid;
        }
        public GetChannelException(string name, int cid) : base("Could not find channel " + name + ". ID: " + cid){
            ChannelId = cid;
            ChannelName = name;
        }
    }
}

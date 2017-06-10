using System;
using TeamspeakBotv2.Core;
using TeamspeakBotv2.Models;

namespace TeamspeakBotv2.Commands
{
    public class GetClientCommand : Command
    {
        public ClientModel Result { get; private set; }
        private string _name;
        private int clid = -1;
        public GetClientCommand(int clid) : this(){
            this.clid = clid;
        }
        public GetClientCommand(string name) : this()
        {
            _name = name;
        }
        private GetClientCommand()
        {
            Message = "clientlist";
        }
        public override void HandleResponse(string msg)
        {
            var cls  = msg.Split('|');
            for(int i = 0; i < cls.Length; i++){
                var m = RegPatterns.Client.Match(cls[i]);
                if(m.Success){
                    var mo = new ClientModel(m);
                    if(clid != -1 && mo.ClientId == clid){
                        Result = mo;
                        return;
                    } else if (!string.IsNullOrEmpty(_name) && mo.ClientName == _name){
                        Result = mo;
                        return;
                    }
                }
                else
                {
                    _failed("Failed to match regex.");
                    throw new RegexMatchException();
                }
            }
            _failed("Could not find user.");
        }
    }
}
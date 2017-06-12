using System;
using TeamspeakBotv2.Core;
using TeamspeakBotv2.Models;

namespace TeamspeakBotv2.Commands
{
    public class GetClientCommand : Command
    {
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
                        break;
                    } else if (!string.IsNullOrEmpty(_name) && mo.ClientName.ToLower() == _name.ToLower()){
                        Result = mo;
                        break;
                    }
                }
                else
                {
                    throw new RegexMatchException(msg, RegPatterns.Client);
                }
            }
            if (Result == null)
            {
                _failed("Could not find user.");
                return;
            }

            base.HandleResponse(msg);
        }
    }
}
using System;
using TeamspeakBotv2.Core;
using TeamspeakBotv2.Models;

namespace TeamspeakBotv2.Commands
{
    public class WhoAmICommand : Command
    {
        public WhoAmICommand(){
            Message = "whoami";
        }
        public override void HandleResponse(string msg)
        {
            var m = RegPatterns.WhoAmI.Match(msg);
            if(m.Success){
                Result = new WhoAmIModel(m);
            } else {
                throw new RegexMatchException();
            }

            base.HandleResponse(msg);
        }
    }
}
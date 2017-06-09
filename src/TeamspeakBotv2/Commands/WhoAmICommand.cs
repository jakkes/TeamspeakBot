using System;
using TeamspeakBotv2.Core;
using TeamspeakBotv2.Models;

namespace TeamspeakBotv2.Commands
{
    public class WhoAmICommand : Command
    {
        public WhoAmIModel Result { get; private set; }
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
        }
    }

    public class WhoAmIException : Exception{
        public WhoAmIException(string msg) : base(msg)
        {
            
        }
        public WhoAmIException() : base("Failed to retreive who am i info")
        {
            
        }
    }
}
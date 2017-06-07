using System;
using TeamspeakBotv2.Core;
using TeamspeakBotv2.Models;

namespace TeamspeakBotv2.Commands
{
    public class SendTextCommand : Command
    {
        public SendTextCommand(string msg)
        {
            Message = string.Format(@"sendtextmessage targetmode=2 msg=\n{0}", msg.Replace(" ", "\\s"));
        }
        public override void HandleResponse(string msg)
        {
            
        }
    }
    public class SendTextCommandException : Exception
    {
        public SendTextCommandException() : base("Failed to send text message.")
        {

        }
        public SendTextCommandException(string msg): base(msg)
        {

        }
    }
}

using System;

namespace TeamspeakBotv2.Commands
{
    public class RegisterToEventCommand : Command
    {
        public RegisterToEventCommand(Event ev, int cid)
        {
            if (ev == Event.Channel)
                Message = "servernotifyregister event=channel id=" + cid;
            else if (ev == Event.TextChannel)
                Message = "servernotifyregister event=textchannel";
        }
        public override void HandleResponse(string msg)
        {
        }
    }
    public enum Event
    {
        Channel,
        TextChannel
    }
}
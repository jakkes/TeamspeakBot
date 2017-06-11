using System;

namespace TeamspeakBotv2.Commands
{
    public class RegisterToEventCommand : NonCollectCommand
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
            throw new RegexMatchException();
        }
    }
    public enum Event
    {
        Channel,
        TextChannel
    }
}
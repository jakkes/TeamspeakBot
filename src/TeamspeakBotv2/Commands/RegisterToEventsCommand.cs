using System;

namespace TeamspeakBotv2.Commands
{
    public class RegisterToEventCommand : Command
    {
        public RegisterToEventCommand(Event ev, int cid){
            if(ev == Event.Channel)
                Message = "servernotifyregister event=channel id=" + cid;
            else if(ev == Event.TextChannel)
                Message = "servernotifyregister event=textchannel";
        }
        public override void HandleResponse(string msg)
        {
        }
    }
    public enum Event{
            Channel,
            TextChannel
        }
    public class RegisterToEventException : Exception{
        public Event Ev { get; set; }
        public RegisterToEventException(string msg, Event ev) : base(msg){
            Ev = ev;
        }
        public RegisterToEventException(Event ev) : base("Failed to register to event."){
            Ev = ev;
        }
    }
}
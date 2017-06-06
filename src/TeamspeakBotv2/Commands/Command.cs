using System;
using System.Threading;

namespace TeamspeakBotv2.Commands
{
    public abstract class Command
    {
        public string Message { get; protected set; }
        public string ErrorMessage { get; protected set; }
        public abstract void HandleResponse(string msg);

        public AutoResetEvent Success = new AutoResetEvent(false);
        public AutoResetEvent Failed = new AutoResetEvent(false);
    }
}
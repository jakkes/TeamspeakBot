using System;
using System.Threading;

namespace TeamspeakBotv2.Commands
{
    public abstract class Command
    {
        public string Message { get; protected set; }
        public string ErrorMessage { get; protected set; }
        public abstract void HandleResponse(string msg);

        public ManualResetEvent Success = new ManualResetEvent(false);
        public ManualResetEvent Failed = new ManualResetEvent(false);

        protected void _failed(string msg){
            ErrorMessage = msg;
            Failed.Set();
        }

        /// <summary>
        /// Waits for the result.
        /// </summary>
        /// <returns>Returns true if the command was successful and false if an error was encountered.</returns>
        public bool Succeeded(int timeout){
            return WaitHandle.WaitAny(new WaitHandle[]{ Success, Failed}, timeout) == 0;
        }
    }
}
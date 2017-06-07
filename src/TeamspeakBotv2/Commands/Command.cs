using System;
using System.Threading;
using TeamspeakBotv2.Core;
using TeamspeakBotv2.Models;

namespace TeamspeakBotv2.Commands
{
    public abstract class Command
    {
        public string Message { get; protected set; }
        public string ErrorMessage { get; protected set; }
        public ErrorModel Error { get; protected set; }
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
            if(timeout == -1)
                return WaitHandle.WaitAny(new WaitHandle[]{ Success, Failed}) == 0;
            else
                return WaitHandle.WaitAny(new WaitHandle[]{ Success, Failed}, timeout) == 0;
        }

        public void HandleErrorLine(ErrorModel mo){
            Error = mo;
            if (!mo.Error)
                Success.Set();
            else
                _failed(mo.Message);
        }
    }
    public class RegexMatchException : Exception
    {
        public RegexMatchException(string msg) : base(msg) { }
        public RegexMatchException() : base("Failed to match regex.")
        {

        }
    }
}
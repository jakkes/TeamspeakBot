using System;
using System.Text.RegularExpressions;
using System.Threading;
using TeamspeakBotv2.Core;
using TeamspeakBotv2.Models;

namespace TeamspeakBotv2.Commands
{
    public abstract class Command
    {
        public object Result { get; set; }
        internal string Message { get; set; }
        public string ErrorMessage { get; protected set; }
        public ErrorModel Error { get; protected set; }
        public virtual void HandleResponse(string msg)
        {
            if (Error != null)
            {
                if (!Error.Error)
                    Success.Set();
                else
                    _failed(Error.Message);
            }
        }

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
            if (timeout == -1)
                return WaitHandle.WaitAny(new WaitHandle[] { Success, Failed }) == 0;
            else
            {
                var r = WaitHandle.WaitAny(new WaitHandle[] { Success, Failed }, timeout);
                if (r == WaitHandle.WaitTimeout)
                    throw new TimeoutException();
                else return r == 0;
            }
        }

        public void HandleErrorLine(ErrorModel mo){
            if (Error != null)
                throw new ErrorPreviouslyHandledException();

            Error = mo;
            if (Result != null)
            {
                if (!mo.Error)
                    Success.Set();
                else
                    _failed(mo.Message);
            }
        }
    }
    public class ErrorPreviouslyHandledException : Exception
    {

    }
    public class RegexMatchException : Exception
    {
        public string Line { get; set; }
        public Regex Regex { get; set; }
        public RegexMatchException(string msg) : base(msg) { }
        public RegexMatchException() : base("Failed to match regex.")
        {

        }
        public RegexMatchException(string line, Regex rgx) : base("Failed to match regex")
        {
            Line = line; Regex = rgx;
        }
    }
    public class CommandException : Exception
    {
        public ErrorModel Error { get; set; }
        public Type Command { get; set; }
        public new string Message { get; set; }
        public CommandException(ErrorModel model, Type command)
        {
            Error = model;
            Command = command;
            if (command == typeof(BanCommand))
                Message = "BanCommand failed";
            else if (command == typeof(CreateChannelCommand))
                Message = "CreateChannelCommand failed";
            else if (command == typeof(GetClientCommand))
                Message = "GetClientCommand failed";
            else if (command == typeof(GetDetailedClientCommand))
                Message = "GetDetailedClientCommand failed";
            else if (command == typeof(GetUIDCommand))
                Message = "GetUIDCommand failed";
            else if (command == typeof(LoginCommand))
                Message = "LoginCommand failed";
            else if (command == typeof(MoveClientCommand))
                Message = "MoveClientCommand failed";
            else if (command == typeof(PokeClientCommand))
                Message = "PokeClientCommand failed";
            else if (command == typeof(RegisterToEventCommand))
                Message = "RegisterForEventCommand failed";
            else if (command == typeof(SelectServerCommand))
                Message = "SelectServerCommand failed";
            else if (command == typeof(SendTextCommand))
                Message = "SentTextCommand failed";
            else if (command == typeof(SetChannelNameCommand))
                Message = "SetChannelNameCommand failed";
            else if (command == typeof(WhoAmICommand))
                Message = "WhoAmICommand failed";
            else
                Message = command.FullName + " failed";
        }
        public CommandException(ErrorModel model, Type command, string message)
        {
            Error = model;
            Command = command;
            Message = message;
        }
    }
}
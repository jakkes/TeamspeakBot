using System;
using TeamspeakBotv2.Models;
using TeamspeakBotv2.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamspeakBotv2.Commands
{
    public class LoginCommand : Command
    {
        

        public override void HandleResponse(string msg)
        {
            var m = RegPatterns.ErrorLine.Match(msg);
            if (m.Success)
            {
                var mo = new ErrorModel(m);
                if (!mo.Error)
                    Success.Set();
                else
                    _failed(mo.Message);
            }
            else
                _failed("Could not match regex.");
        }

        internal void _failed(string msg)
        {
            ErrorMessage = msg;
            Failed.Set();
        }

        public LoginCommand(string username, string password)
        {
            Message = string.Format("login {0} {1}", username, password);
        }
    }

    public class LoginException : Exception
    {
        public LoginException(string msg) : base(msg)
        {
        }
        public LoginException() : base("Failed to login.")
        {
        }
    }
}

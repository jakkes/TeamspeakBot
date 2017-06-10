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
            
        }

        public LoginCommand(string username, string password)
        {
            Message = string.Format("login {0} {1}", username, password);
        }
    }
}

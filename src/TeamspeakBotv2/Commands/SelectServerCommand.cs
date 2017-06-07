using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamspeakBotv2.Core;
using TeamspeakBotv2.Models;

namespace TeamspeakBotv2.Commands
{
    public class SelectServerCommand : Command
    {
        public override void HandleResponse(string msg)
        {
            var m = RegPatterns.ErrorLine.Match(msg);
            if (m.Success)
            {
                var mo = new ErrorModel(m);
                if (!mo.Error)
                    _success();
                else
                    _failed(mo.Message);
            }
            else
                _failed("Could not match regex.");
        }

        private void _success()
        {
            Success.Set();
        }

        private void _failed(string v)
        {
            ErrorMessage = v;
            Failed.Set();
        }

        public SelectServerCommand(int serverId)
        {
            Message = string.Format("use sid={0}", serverId);
        }
    }

    public class SelectServerException : Exception
    {
        public SelectServerException() : base("Failed to select server") { }
    }
}

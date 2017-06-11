using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamspeakBotv2.Core;
using TeamspeakBotv2.Models;

namespace TeamspeakBotv2.Commands
{
    public class SelectServerCommand : NonCollectCommand
    {
        public override void HandleResponse(string msg)
        {
            throw new RegexMatchException();
        }

        public SelectServerCommand(int serverId)
        {
            Message = string.Format("use sid={0}", serverId);
        }
    }
}

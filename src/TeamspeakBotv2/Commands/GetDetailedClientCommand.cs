using System;
using TeamspeakBotv2.Core;
using TeamspeakBotv2.Models;
using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Commands
{
    public class GetDetailedClientCommand : CollectCommand
    {
        private string _name;
        public override void HandleResponse(string msg)
        {
            var m = RegPatterns.DetailedClient.Match(msg);
            if (m.Success)
            {
                DetailedClientModel mo = new DetailedClientModel(m);
                if (mo.ClientName.ToLower() == _name.ToLower())
                    Result = mo;
                else
                    throw new ArgumentException();
            }
            else
                throw new RegexMatchException(msg, RegPatterns.DetailedClient);

            base.HandleResponse(msg);
        }
        public GetDetailedClientCommand(int clid, string name)
        {
            _name = name;
            Message = string.Format("clientinfo clid={0}", clid);
        }
    }
}

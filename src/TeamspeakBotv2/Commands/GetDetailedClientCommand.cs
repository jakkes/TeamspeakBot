using System;
using TeamspeakBotv2.Core;
using TeamspeakBotv2.Models;
using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Commands
{
    public class GetDetailedClientCommand : Command
    {
        public DetailedClientModel Result { get; private set; }
        public override void HandleResponse(string msg)
        {
            var m = RegPatterns.DetailedClient.Match(msg);
            if (m.Success)
            {
                DetailedClientModel mo = new DetailedClientModel(m);
                Result = mo;
            }
            else
            {
                _failed("Failed to match expression");
                throw new RegexMatchException();
            }
        }
        public GetDetailedClientCommand(int clid)
        {
            Message = string.Format("clientinfo clid={0}", clid);
        }
    }

    public class GetDetailedClientException : Exception
    {
        public GetDetailedClientException() : base("Failed to retrieve detailed client information.")
        {

        }
        public GetDetailedClientException(string msg) : base(msg) { }
    }
}

using System;
using TeamspeakBotv2.Core;
using TeamspeakBotv2.Models;

namespace TeamspeakBotv2.Commands
{
    public class GetUIDCommand : CollectCommand
    {
        private int _clid;
        public GetUIDCommand(int clid){
            Message = string.Format("clientgetuidfromclid clid={0}", clid);
            _clid = clid;
        }
        public override void HandleResponse(string msg)
        {
            var m = RegPatterns.ClientUniqueIdFromId.Match(msg);
            if(m.Success){
                var model = new GetUidFromClidModel(m);
                if (model.ClientId == _clid)
                    Result = new GetUidFromClidModel(m).ClientUniqueId;
                else
                    throw new ArgumentException("Wrong response");
            }
            else
            {
                throw new RegexMatchException(msg, RegPatterns.ClientUniqueIdFromId);
            }

            base.HandleResponse(msg);
        }
    }
}
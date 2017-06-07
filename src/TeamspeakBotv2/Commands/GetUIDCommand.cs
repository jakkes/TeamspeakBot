using System;
using TeamspeakBotv2.Core;
using TeamspeakBotv2.Models;

namespace TeamspeakBotv2.Commands
{
    public class GetUIDCommand : Command
    {
        public string Result { get; set; }
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
                if(model.ClientId == _clid)
                    Result = new GetUidFromClidModel(m).ClientUniqueId;
                else
                    _failed("Client ID did not match.");
            }
            else
            {
                _failed("Failed to match regex.");
                throw new RegexMatchException();
            }
        }
    }

    public class GetUIDException : Exception{
        public GetUIDException(string msg) : base(msg){

        }
        public GetUIDException() : base("Failed to retrieve UID."){
            
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamspeakBotv2.Core
{
    public class Config
    {
        private HashSet<string> banList = new HashSet<string>();
        private HashSet<string> whiteList = new HashSet<string>();
        private bool useWhitelist = false;

        public void Ban(string uniqueId) => banList.Add(uniqueId);
        public void Unban(string uniqueId) => banList.RemoveWhere(x => x == uniqueId);
        public void AddToWhitelist(string uniqueId) => whiteList.Add(uniqueId);
        public void RemoveFromWhitelist(string uniqueId) => whiteList.RemoveWhere(x => x == uniqueId);
        public IEnumerable<string> Banlist { get { return banList.ToArray(); } }
        public IEnumerable<string> Whitelist { get { return whiteList.ToArray(); } }
        public bool AllowedInChannel(string uniqueId) => useWhitelist && whiteList.Contains(uniqueId) || !useWhitelist && !banList.Contains(uniqueId);
        public void UseWhitelist() => useWhitelist = true;
        public void UseBanlist() => useWhitelist = false;
    }
}

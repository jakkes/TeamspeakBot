using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TeamspeakBotv2.Core
{
    public class Config
    {
        private Dictionary<string, string> banList = new Dictionary<string, string>();
        private Dictionary<string, string> whiteList = new Dictionary<string, string>();
        private bool useWhitelist = false;

        /// <summary>
        /// Adds a user to the banlist
        /// </summary>
        /// <param name="uniqueId">Unique id</param>
        /// <param name="name">Name</param>
        /// <exception cref="ArgumentException">Throws if the user is banned already.</exception>
        /// <exception cref="ArgumentNullException">Throws if uniqueId is null.</exception>
        public void Ban(string uniqueId, string name) => banList.Add(uniqueId,name);
        public void Unban(string uniqueId) => banList.Remove(uniqueId);
        /// <summary>
        /// Adds a user to the whitelist
        /// </summary>
        /// <param name="uniqueId">Unique id</param>
        /// <param name="name">Name</param>
        /// <exception cref="ArgumentException">Throws if the user is banned already.</exception>
        /// <exception cref="ArgumentNullException">Throws if uniqueId is null.</exception>
        public void AddToWhitelist(string uniqueId,string name) => whiteList.Add(uniqueId,name);
        public void RemoveFromWhitelist(string uniqueId) => whiteList.Remove(uniqueId);
        public IEnumerable<string> Banlist { get { return banList.Values.ToList(); } }
        public IEnumerable<string> Whitelist { get { return whiteList.Values.ToList(); } }
        public bool AllowedInChannel(string uniqueId) => useWhitelist && whiteList.ContainsKey(uniqueId) || !useWhitelist && !banList.ContainsKey(uniqueId);
        public void UseWhitelist() => useWhitelist = true;
        public void UseBanlist() => useWhitelist = false;
    }
}

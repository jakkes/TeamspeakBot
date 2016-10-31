using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace TeamspeakBotv2.Core
{
    public class Config
    {
        public Dictionary<string, string> Banlist { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Whitelist { get; private set; } = new Dictionary<string, string>();
        public bool useWhitelist { get; private set; } = false;

        public void Save(string OwnerUid)
        {
            if (!Directory.Exists("Configs"))
                Directory.CreateDirectory("Configs");
            File.WriteAllText("Configs/" + OwnerUid.Replace("\\","").Replace("/","") + ".json", JsonConvert.SerializeObject(this));
        }
        /// <summary>
        /// Adds a user to the banlist
        /// </summary>
        /// <param name="uniqueId">Unique id</param>
        /// <param name="name">Name</param>
        /// <exception cref="ArgumentException">Throws if the user is banned already.</exception>
        /// <exception cref="ArgumentNullException">Throws if uniqueId is null.</exception>
        public void Ban(string uniqueId, string name) => Banlist.Add(uniqueId,name);
        public void ClearBanlist() { Banlist.Clear(); }
        public void Unban(string uniqueId) => Banlist.Remove(uniqueId);
        /// <summary>
        /// Adds a user to the whitelist
        /// </summary>
        /// <param name="uniqueId">Unique id</param>
        /// <param name="name">Name</param>
        /// <exception cref="ArgumentException">Throws if the user is banned already.</exception>
        /// <exception cref="ArgumentNullException">Throws if uniqueId is null.</exception>
        public void AddToWhitelist(string uniqueId,string name) => Whitelist.Add(uniqueId,name);
        public void RemoveFromWhitelist(string uniqueId) => Whitelist.Remove(uniqueId);
        public bool AllowedInChannel(string uniqueId) => useWhitelist && Whitelist.ContainsKey(uniqueId) || !useWhitelist && !Banlist.ContainsKey(uniqueId);
        public void UseWhitelist() => useWhitelist = true;
        public void UseBanlist() => useWhitelist = false;
        public static Config Load(string uniqueId)
        {
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText("Configs/" + uniqueId.Replace("\\", "").Replace("/", "") + ".json"));
        }
    }
}

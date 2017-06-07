using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamspeakBotv2.Config
{
    public class ServerConfig
    {
        public int Id { get; set; }
        public string DefaultChannel { get; set; }
        public string[] Channels { get; set; }
        public string Parent { get; set; }
        public int BanTime { get; set; }
    }
}

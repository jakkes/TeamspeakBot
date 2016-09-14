using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamspeakBotv2.Config
{
    public class HostConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public ServerConfig[] Servers { get; set; }
    }
}

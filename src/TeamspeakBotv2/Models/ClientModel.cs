using System;
using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Models
{
    public class ClientModel : IUser
    {
        public int ClientId { get; set; }
        public int ChannelId { get; set; }
        public int DatabaseId { get; set; }
        public string ClientName { get; set; }
        public string UniqueId { get; set; }
        public ClientModel(Match m)
        {
            ClientId = int.Parse(m.Groups[1].Value);
            ChannelId = int.Parse(m.Groups[2].Value);
            DatabaseId = int.Parse(m.Groups[3].Value);
            ClientName = m.Groups[4].Value;
        }
    }
}

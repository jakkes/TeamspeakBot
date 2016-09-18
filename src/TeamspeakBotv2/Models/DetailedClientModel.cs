using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Models
{
    public class DetailedClientModel
    {
        public string ClientName { get; set; }
        public string UniqueId { get; set; }
        public int ChannelId { get; set; }
        public DetailedClientModel(Match m)
        {
            ChannelId = int.Parse(m.Groups[1].Value);
            UniqueId = m.Groups[3].Value;
            ClientName = m.Groups[4].Value;
        }
    }
}

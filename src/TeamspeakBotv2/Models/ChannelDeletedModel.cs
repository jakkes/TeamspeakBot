using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Models
{
    public class ChannelDeletedModel
    {

        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public string UniqueId { get; set; }
        public int ChannelId { get; set; }

        public ChannelDeletedModel(Match m)
        {
            ClientId = int.Parse(m.Groups[1].Value);
            ClientName = m.Groups[2].Value;
            UniqueId = m.Groups[3].Value;
            ChannelId = int.Parse(m.Groups[4].Value);
        }
    }
}

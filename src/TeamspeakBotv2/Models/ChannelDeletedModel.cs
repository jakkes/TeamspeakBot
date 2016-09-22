using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Models
{
    public class ChannelDeletedModel
    {

        public int ClientId { get; set; }
        public int ChannelId { get; set; }

        public ChannelDeletedModel(Match m)
        {
            ClientId = int.Parse(m.Groups[1].Value);
            ChannelId = int.Parse(m.Groups[2].Value);
        }
    }
}

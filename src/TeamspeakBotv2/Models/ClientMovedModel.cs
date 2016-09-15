using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Models
{
    public class ClientMovedModel
    {
        public int ChannelToId { get; set; }
        public int ReasonId { get; set; }
        public int ClientId { get; set; }
        public ClientMovedModel(Match m)
        {
            ChannelToId = int.Parse(m.Groups[1].Value);
            ReasonId = int.Parse(m.Groups[2].Value);
            ClientId = int.Parse(m.Groups[3].Value);
        }
    }
}

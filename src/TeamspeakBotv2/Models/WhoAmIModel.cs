using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Models
{
    public class WhoAmIModel : IUser
    {
        public int ClientId { get; set; }
        public int ChannelId { get; set; }

        public WhoAmIModel(Match match)
        {
            ClientId = int.Parse(match.Groups[1].Value);
            ChannelId = int.Parse(match.Groups[2].Value);
        }
    }
}

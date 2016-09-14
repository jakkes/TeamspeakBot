using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Models
{
    public class WhoAmIModel
    {
        public int ClientId { get; set; }

        public WhoAmIModel(Match match)
        {
            ClientId = int.Parse(match.Groups[1].Value);
        }
    }
}

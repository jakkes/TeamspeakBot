using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Models
{
    public class GetUidFromClidModel
    {
        public int ClientId { get; set; }
        public string ClientUniqueId { get; set; }

        public GetUidFromClidModel(Match m)
        {
            ClientId = int.Parse(m.Groups[1].Value);
            ClientUniqueId = m.Groups[2].Value;
        }
    }
}

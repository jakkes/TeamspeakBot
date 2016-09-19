using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Models
{
    public class ClientFindModel
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public ClientFindModel(Match m)
        {
            ClientId = int.Parse(m.Groups[1].Value);
            ClientName = m.Groups[2].Value.Replace("\\s"," ");
        }
    }
}

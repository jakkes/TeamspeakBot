using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Models
{
    public class ClientLeftViewModel
    {
        public int ChannelFromId { get; set; }
        public int ChannelToId { get; set; }
        public int ReasonId { get; set; }
        public string ReasonMessage { get; set; }
        public int ClientId { get; set; }
        public ClientLeftViewModel(Match m)
        {
            ChannelFromId = int.Parse(m.Groups[1].Value);
            ChannelToId = int.Parse(m.Groups[2].Value);
            ReasonId = int.Parse(m.Groups[3].Value);
            ReasonMessage = m.Groups[4].Value.Replace("\\s"," ");
            ClientId = int.Parse(m.Groups[5].Value);
        }
    }
}

using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Models
{
    public class MessageModel
    {
        public string[] Words { get; set; }
        public int TargetMode { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientUniqueId { get; set; }
        public MessageModel(Match m)
        {
            TargetMode = int.Parse(m.Groups[1].Value);
            Words = m.Groups[2].Value.Split(new string[] { "\\s" }, System.StringSplitOptions.RemoveEmptyEntries);
            ClientId = int.Parse(m.Groups[3].Value);
            ClientName = m.Groups[4].Value;
            ClientUniqueId = m.Groups[5].Value;
        }
    }
}

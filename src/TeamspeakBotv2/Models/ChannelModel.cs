using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Models
{
    public class ChannelModel
    {

        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        
        public ChannelModel(Match m)
        {
            ChannelId = int.Parse(m.Groups[1].Value);
            ChannelName = m.Groups[2].Value.Replace("\\s"," ");
        }
        public ChannelModel(){}
    }
}

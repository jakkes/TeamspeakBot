using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TeamspeakBotv2.Models
{
    public class ClientEnteredViewModel
    {
        public int FromChannelId { get; set; }
        public int ToChannelId { get; set; }
        public int ClientId { get; set; }
        public int ReasonId { get; set; }

        public ClientEnteredViewModel(Match m)
        {
            FromChannelId = int.Parse(m.Groups[1].Value);
            ToChannelId = int.Parse(m.Groups[2].Value);
            ClientId = int.Parse(m.Groups[3].Value);
            ReasonId = int.Parse(m.Groups[4].Value);
        }
    }
}

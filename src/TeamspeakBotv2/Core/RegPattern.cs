using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Core
{
    public class RegPatterns
    {
        public static Regex ErrorLine = new Regex(@"^error id=([0-9]+) msg=([^ ]*)");
        public static Regex EnterView = new Regex(@"^notifycliententerview cfid=([0-9]+) ctid=([0-9]+) reasonid=([0-9]+) clid=([0-9]+)");
        public static Regex WhoAmI = new Regex(@"client_id=([0-9]*)");
        public static Regex Channel = new Regex(@"cid=(\d+) pid=[0-9]+ channel_order=\d+ channel_name=([^ ]*)");
        public static Regex ClientUniqueIdFromId = new Regex(@"clid=(\d+) cluid=([^ ]+) nickname=[^ ]");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Core
{
    public class RegPatterns
    {
        public static Regex ErrorLine = new Regex(@"error id=([0-9]+) msg=([^ ]*)");
        public static Regex EnterView = new Regex("notifycliententerview cfid=([0-9]+) ctid=([0-9]+) reasonid=([0-9]+) clid=([0-9]+)");
    }
}

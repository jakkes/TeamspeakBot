using System;

namespace TeamspeakBotv2.Core
{
    public class Command
    {
        public string Message { get; set; }
        public Action onSent { get; set; }
        public Action onFail { get; set; }
        public Action onSuccess { get; set; }
        public int Timeout { get; set; }
        /// <summary>
        /// Command to send to the query.
        /// </summary>
        /// <param name="msg">Message to send.</param>
        public Command(string msg){
            Message = msg;
        }
    }
}
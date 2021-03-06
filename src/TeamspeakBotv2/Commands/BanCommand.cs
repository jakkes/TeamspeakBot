﻿using System;
using TeamspeakBotv2.Core;
using TeamspeakBotv2.Models;

namespace TeamspeakBotv2.Commands
{
    public class BanCommand : NonCollectCommand
    {
        /// <summary>
        /// Creates a ban command.
        /// </summary>
        /// <param name="clid">Client id</param>
        /// <param name="time">Ban time in seconds</param>
        public BanCommand(int clid, int time)
        {
            Message = string.Format("banclient clid={0} time={1}", clid, time);
        }
        public override void HandleResponse(string msg)
        {
            throw new RegexMatchException();
        }
    }
}
﻿/* This file is part of TRBot.
 *
 * TRBot is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * TRBot is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with TRBot.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TRBot.Connection;
using TRBot.Permissions;
using TRBot.Data;

namespace TRBot.Commands
{
    /// <summary>
    /// Allows a user to bet credits for a chance to win.
    /// </summary>
    public class BetCommand : BaseCommand
    {
        private string UsageMessage = "Usage: \"bet amount (int)\"";
        private Random Rand = new Random();

        public BetCommand()
        {

        }

        public override void ExecuteCommand(EvtChatCommandArgs args)
        {
            List<string> arguments = args.Command.ArgumentsAsList;

            using BotDBContext context = DatabaseManager.OpenContext();

            string creditsName = DataHelper.GetCreditsNameNoOpen(context);

            string userName = args.Command.ChatMessage.Username;
            User bettingUser = DataHelper.GetUserNoOpen(args.Command.ChatMessage.Username, context);

            if (bettingUser.HasAbility(PermissionConstants.BET_ABILITY) == false)
            {
                QueueMessage("You do not have the ability to bet!");
                return;
            }

            if (bettingUser.IsOptedOut == true)
            {
                QueueMessage("You cannot bet while opted out of stats.");
                return;
            }

            if (arguments.Count != 1)
            {
                QueueMessage(UsageMessage);
                return;
            }

            if (long.TryParse(arguments[0], out long creditBet) == false)
            {
                QueueMessage("Please enter a valid bet amount!");
                return;
            }

            if (creditBet <= 0)
            {
                QueueMessage("Bet amount must be greater than 0!");
                return;
            }

            if (creditBet > bettingUser.Stats.Credits)
            {
                QueueMessage($"Bet amount is greater than {creditsName}!");
                return;
            }

            //Make it a 50/50 chance
            bool success = (Rand.Next(0, 2) == 0);
            string message = string.Empty;
                
            //Add or subtract credits based on the bet result
            if (success)
            {
                bettingUser.Stats.Credits += creditBet;
                message = $"{bettingUser.Name} won {creditBet} {creditsName} PogChamp";
            }
            else
            {
                bettingUser.Stats.Credits -= creditBet;
                message = $"{bettingUser.Name} lost {creditBet} {creditsName} BibleThump";
            }

            context.SaveChanges();
                
            QueueMessage(message);
        }
    }
}

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
using System.Globalization;
using TRBot.Connection;
using TRBot.Misc;
using TRBot.Utilities;
using TRBot.Consoles;
using TRBot.Data;
using TRBot.Permissions;

namespace TRBot.Commands
{
    /// <summary>
    /// A command that removes an input from the invalid input combo for a console.
    /// </summary>
    public sealed class RemoveInvalidInputComboCommand : BaseCommand
    {
        private string UsageMessage = $"Usage - \"console name (string)\", \"input name (string)\"";

        public RemoveInvalidInputComboCommand()
        {
            
        }

        public override void ExecuteCommand(EvtChatCommandArgs args)
        {
            List<string> arguments = args.Command.ArgumentsAsList;

            int argCount = arguments.Count;

            //Ignore with not enough arguments
            if (argCount != 2)
            {
                QueueMessage(UsageMessage);
                return;
            }

            string consoleStr = arguments[0].ToLowerInvariant();
            string inputName = arguments[1].ToLowerInvariant();

            using (BotDBContext context = DatabaseManager.OpenContext())
            {
                GameConsole console = context.Consoles.FirstOrDefault(c => c.Name == consoleStr);

                if (console == null)
                {
                    QueueMessage($"No console named \"{consoleStr}\" found.");
                    return;
                }

                //Check if the input exists
                InputData existingInput = console.InputList.FirstOrDefault((inpData) => inpData.Name == inputName);

                if (existingInput == null)
                {
                    QueueMessage($"No input named \"{inputName}\" exists for the \"{console.Name}\" console!");
                    return;
                }

                //Check if it's not in the invalid input combo
                InvalidCombo existingCombo = console.InvalidCombos.FirstOrDefault((inpCombo) => inpCombo.Input.ID == existingInput.ID);

                if (existingCombo == null)
                {
                    QueueMessage($"Input \"{inputName}\" is not part of the invalid input combo for the \"{console.Name}\" console!");
                    return;
                }

                //Remove the combo from the invalid input combo for this console and save
                console.InvalidCombos.Remove(existingCombo);

                context.SaveChanges();
            }

            QueueMessage($"Successfully removed \"{inputName}\" from the invalid input combo for the \"{consoleStr}\" console!");
        }
    }
}

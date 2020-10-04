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
using TRBot.Common;
using TRBot.Utilities;
using TRBot.Data;

namespace TRBot.Commands
{
    /// <summary>
    /// A command that removes a command.
    /// </summary>
    public sealed class RemoveCmdCommand : BaseCommand
    {
        private CommandHandler CmdHandler = null;
        private BotMessageHandler MessageHandler = null;
        private string UsageMessage = $"Usage - \"command name\"";

        public RemoveCmdCommand()
        {
            
        }

        public override void Initialize(CommandHandler cmdHandler, DataContainer dataContainer)
        {
            CmdHandler = cmdHandler;
            MessageHandler = dataContainer.MessageHandler;
        }

        public override void CleanUp()
        {
            CmdHandler = null;
            MessageHandler = null;
        }

        public override void ExecuteCommand(EvtChatCommandArgs args)
        {
            List<string> arguments = args.Command.ArgumentsAsList;

            //Ignore with incorrect number of arguments
            if (arguments.Count != 1)
            {
                MessageHandler.QueueMessage(UsageMessage);
                return;
            }

            string commandName = arguments[0].ToLowerInvariant();

            bool removed = CmdHandler.RemoveCommand(commandName);

            if (removed == true)
            {
                //Remove this command from the database
                using (BotDBContext context = DatabaseManager.OpenContext())
                {
                    CommandData cmdData = context.Commands.FirstOrDefault((cmd) => cmd.name == commandName);
                    if (cmdData != null)
                    {
                        context.Commands.Remove(cmdData);

                        context.SaveChanges();
                    }
                    else
                    {
                        Console.WriteLine($"Error: Command \"{commandName}\" was not removed from the database because it cannot be found.");
                    }
                }

                MessageHandler.QueueMessage($"Successfully removed command \"{commandName}\"!");
            }
            else
            {
                MessageHandler.QueueMessage($"Failed to remove command \"{commandName}\". It likely does not exist.");
            }
        }
    }
}

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
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TRBot.Connection;
using TRBot.Data;
using TRBot.Misc;
using TRBot.Utilities;

namespace TRBot.Commands
{
    /// <summary>
    /// Manages commands.
    /// </summary>
    public class CommandHandler
    {
        private ConcurrentDictionary<string, BaseCommand> AllCommands = new ConcurrentDictionary<string, BaseCommand>(Environment.ProcessorCount * 2, 32);
        private DataContainer DataContainer = null;

        public CommandHandler()
        {

        }

        public void Initialize(DataContainer dataContainer)
        {
            DataContainer = dataContainer;

            DataContainer.DataReloader.SoftDataReloadedEvent -= OnDataReloadedSoft;
            DataContainer.DataReloader.SoftDataReloadedEvent += OnDataReloadedSoft;

            DataContainer.DataReloader.HardDataReloadedEvent -= OnDataReloadedHard;
            DataContainer.DataReloader.HardDataReloadedEvent += OnDataReloadedHard;

            PopulateCommandsFromDB();            
            InitializeCommands();
        }

        public void CleanUp()
        {
            DataContainer.DataReloader.SoftDataReloadedEvent -= OnDataReloadedSoft;
            DataContainer.DataReloader.HardDataReloadedEvent -= OnDataReloadedHard;

            DataContainer = null;

            CleanUpCommands();
        }

        public void HandleCommand(EvtChatCommandArgs args)
        {
            if (args == null || args.Command == null || args.Command.ChatMessage == null)
            {
                DataContainer.MessageHandler.QueueMessage($"{nameof(EvtChatCommandArgs)} or its Command or ChatMessage is null! Not parsing command");
                return;
            }

            string commandToLower = args.Command.CommandText.ToLower();

            if (AllCommands.TryGetValue(commandToLower, out BaseCommand command) == true)
            {
                if (command == null)
                {
                    DataContainer.MessageHandler.QueueMessage($"Command {commandToLower} is null! Not executing.");
                    return;
                }

                //Return if the command is disabled
                if (command.Enabled == false)
                {
                    return;
                }

                //Execute the command
                command.ExecuteCommand(args);
            }
        }

        public BaseCommand GetCommand(string commandName)
        {
            AllCommands.TryGetValue(commandName, out BaseCommand command);

            return command;
        }

        public bool AddCommand(string commandName, string commandTypeName, string valueStr,
            in int level, in bool commandEnabled, in bool displayInHelp)
        {
            Type commandType = Type.GetType(commandTypeName, false, true);
            if (commandType == null)
            {
                DataContainer.MessageHandler.QueueMessage($"Cannot find command type \"{commandTypeName}\" for command \"{commandName}\".");
                return false;
            }

            BaseCommand command = null;

            //Try to create an instance
            try
            {
                command = (BaseCommand)Activator.CreateInstance(commandType, Array.Empty<object>());
                command.Enabled = commandEnabled;
                command.DisplayInHelp = displayInHelp;
                command.Level = level;
                command.ValueStr = valueStr;
            }
            catch (Exception e)
            {
                DataContainer.MessageHandler.QueueMessage($"Unable to add command \"{commandName}\": \"{e.Message}\"");
            }

            return AddCommand(commandName, command);
        }

        public bool AddCommand(string commandName, BaseCommand command)
        {
            if (command == null)
            {
                Console.WriteLine("Cannot add null command.");
                return false;
            }

            //Clean up the existing command before overwriting it with the new value
            if (AllCommands.TryGetValue(commandName, out BaseCommand existingCmd) == true)
            {
                existingCmd.CleanUp();
            }

            //Set and initialize the command
            AllCommands[commandName] = command;
            AllCommands[commandName].Initialize(this, DataContainer);

            return true;
        }

        public bool RemoveCommand(string commandName)
        {
            bool removed = AllCommands.Remove(commandName, out BaseCommand command);
            
            //Clean up the command
            command?.CleanUp();

            return removed;
        }

        private void InitializeCommands()
        {
            foreach (KeyValuePair<string, BaseCommand> cmd in AllCommands)
            {
                cmd.Value.Initialize(this, DataContainer);
            }
        }

        private void CleanUpCommands()
        {
            foreach (KeyValuePair<string, BaseCommand> cmd in AllCommands)
            {
                cmd.Value.CleanUp();
            }
        }

        private void PopulateCommandsFromDB()
        {
            using (BotDBContext context = DatabaseManager.OpenContext())
            {
                foreach (CommandData cmdData in context.Commands)
                {
                    //Find the type corresponding to this class name
                    Type commandType = Type.GetType(cmdData.class_name, false, true);
                    if (commandType == null)
                    {
                        DataContainer.MessageHandler.QueueMessage($"Cannot find command type \"{cmdData.class_name}\" - skipping.");
                        continue;
                    }

                    //Create the type
                    try
                    {
                        BaseCommand baseCmd = (BaseCommand)Activator.CreateInstance(commandType, Array.Empty<object>());
                        baseCmd.Enabled = cmdData.enabled > 0;
                        baseCmd.DisplayInHelp = cmdData.display_in_list > 0;
                        baseCmd.Level = cmdData.level;
                        baseCmd.ValueStr = cmdData.value_str;

                        AllCommands[cmdData.name] = baseCmd;
                    }
                    catch (Exception e)
                    {
                        DataContainer.MessageHandler.QueueMessage($"Unable to create class type \"{cmdData.class_name}\": {e.Message}");
                    }
                }
            }
        }

        private void OnDataReloadedSoft()
        {
            UpdateCommandsFromDB();
        }

        private void OnDataReloadedHard()
        {
            //Clean up and clear all commands
            CleanUpCommands();
            AllCommands.Clear();

            PopulateCommandsFromDB();

            //Re-initialize all commands
            InitializeCommands();
        }

        private void UpdateCommandsFromDB()
        {
            using (BotDBContext context = DatabaseManager.OpenContext())
            {
                List<string> encounteredCommands = new List<string>(context.Commands.Count());

                foreach (CommandData cmdData in context.Commands)
                {
                    string commandName = cmdData.name;
                    if (AllCommands.TryGetValue(commandName, out BaseCommand baseCmd) == true)
                    {
                        //Remove this command if the type name is different so we can reconstruct it
                        if (baseCmd.GetType().FullName != cmdData.class_name)
                        {
                            RemoveCommand(commandName);
                        }

                        baseCmd = null;
                    }

                    //Add this command if it doesn't exist and should
                    if (baseCmd == null)
                    {
                        //Add this command
                        AddCommand(commandName, cmdData.class_name, cmdData.value_str,
                            cmdData.level, cmdData.enabled != 0, cmdData.display_in_list != 0 );
                    }
                    else
                    {
                        baseCmd.Level = cmdData.level;
                        baseCmd.Enabled = cmdData.enabled != 0;
                        baseCmd.DisplayInHelp = cmdData.display_in_list != 0;
                        baseCmd.ValueStr = cmdData.value_str;
                    }

                    encounteredCommands.Add(commandName);
                }

                //Remove commands that are no longer in the database
                foreach (string cmd in AllCommands.Keys)
                {
                    if (encounteredCommands.Contains(cmd) == false)
                    {
                        RemoveCommand(cmd);
                    }
                }
            }
        }
    }
}

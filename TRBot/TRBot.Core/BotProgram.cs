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
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TRBot.Parsing;
using TRBot.ParserData;
using TRBot.Connection;
using TRBot.Consoles;
using TRBot.VirtualControllers;
using Newtonsoft.Json;

namespace TRBot.Core
{
    public sealed class BotProgram : IDisposable
    {
        private static BotProgram instance = null;

        public bool Initialized { get; private set; } = false;

        public IClientService ClientService { get; private set; } = null;
        public IVirtualControllerManager ControllerMngr { get; private set; } = null;
        public GameConsole CurConsole { get; private set; } = null;

        private InputMacroCollection MacroData = new InputMacroCollection();
        private InputSynonymCollection SynonymData = new InputSynonymCollection();

        private Parser InputParser = null;

        public BotProgram()
        {
            //Below normal priority
            //NOTE: Have a setting to set the priority
            Process thisProcess = Process.GetCurrentProcess();
            thisProcess.PriorityBoostEnabled = false;
            thisProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
        }

        //Clean up anything we need to here
        public void Dispose()
        {
            if (Initialized == false)
                return;

            UnsubscribeEvents();

            //RoutineHandler?.CleanUp();

            //CommandHandler.CleanUp();
            ClientService?.CleanUp();

            //MsgHandler?.CleanUp();

            if (ClientService?.IsConnected == true)
                ClientService.Disconnect();

            //Clean up and relinquish the virtual controllers when we're done
            ControllerMngr?.CleanUp();

            instance = null;
        }

        public void Initialize()
        {
            if (Initialized == true)
                return;

            //Kimimaru: Use invariant culture
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            Console.WriteLine($"Setting up client service: Terminal");

            InputParser = new Parser();

            CurConsole = new GCConsole();

            ClientService = new TerminalClientService('!');

            //Initialize service
            ClientService.Initialize();

            UnsubscribeEvents();
            SubscribeEvents();

            ControllerMngr = new UInputControllerManager();
            ControllerMngr.Initialize();
            ControllerMngr.InitControllers(1);

            MacroData = new InputMacroCollection(new ConcurrentDictionary<string, InputMacro>());
            MacroData.AddMacro(new InputMacro("#mash(*)", "[<0>34ms #34ms]*20"));
            MacroData.AddMacro(new InputMacro("#test", "b500ms #200ms up"));
            MacroData.AddMacro(new InputMacro("#test2", "a #200ms #test"));

            SynonymData = new InputSynonymCollection(new ConcurrentDictionary<string, InputSynonym>());
            SynonymData.AddSynonym(new InputSynonym(".", "#"));
            SynonymData.AddSynonym(new InputSynonym("aandup", "a+up"));

            Console.WriteLine($"Setting up virtual controller uinput with {ControllerMngr.ControllerCount} controllers");

            Initialized = true;
        }

        public void Run()
        {
            if (ClientService.IsConnected == true)
            {
                Console.WriteLine("Client is already connected and running!");
                return;
            }

            ClientService.Connect();

            //Run
            while (true)
            {
                //Store the bot's uptime
                DateTime utcNow = DateTime.UtcNow;
                //TotalUptime = (utcNow - StartUptime);

                DateTime now = DateTime.Now;

                //Update queued messages
                //MsgHandler.Update(now);

                //Update routines
                //RoutineHandler.Update(now);

                Thread.Sleep(100);
            }
        }

        private void UnsubscribeEvents()
        {
            ClientService.EventHandler.UserSentMessageEvent -= OnUserSentMessage;
            //ClientService.EventHandler.UserMadeInputEvent -= OnUserMadeInput;
            ClientService.EventHandler.UserNewlySubscribedEvent -= OnNewSubscriber;
            ClientService.EventHandler.UserReSubscribedEvent -= OnReSubscriber;
            ClientService.EventHandler.WhisperReceivedEvent -= OnWhisperReceived;
            ClientService.EventHandler.ChatCommandReceivedEvent -= OnChatCommandReceived;
            ClientService.EventHandler.OnJoinedChannelEvent -= OnJoinedChannel;
            ClientService.EventHandler.ChannelHostedEvent -= OnBeingHosted;
            ClientService.EventHandler.OnConnectedEvent -= OnConnected;
            ClientService.EventHandler.OnConnectionErrorEvent -= OnConnectionError;
            ClientService.EventHandler.OnReconnectedEvent -= OnReconnected;
            ClientService.EventHandler.OnDisconnectedEvent -= OnDisconnected;
        }

        private void SubscribeEvents()
        {
            ClientService.EventHandler.UserSentMessageEvent += OnUserSentMessage;
            //ClientService.EventHandler.UserMadeInputEvent += OnUserMadeInput;
            ClientService.EventHandler.UserNewlySubscribedEvent += OnNewSubscriber;
            ClientService.EventHandler.UserReSubscribedEvent += OnReSubscriber;
            ClientService.EventHandler.WhisperReceivedEvent += OnWhisperReceived;
            ClientService.EventHandler.ChatCommandReceivedEvent += OnChatCommandReceived;
            ClientService.EventHandler.OnJoinedChannelEvent += OnJoinedChannel;
            ClientService.EventHandler.ChannelHostedEvent += OnBeingHosted;
            ClientService.EventHandler.OnConnectedEvent += OnConnected;
            ClientService.EventHandler.OnConnectionErrorEvent += OnConnectionError;
            ClientService.EventHandler.OnReconnectedEvent += OnReconnected;
            ClientService.EventHandler.OnDisconnectedEvent += OnDisconnected;
        }

#region Events

        private void OnConnected(EvtConnectedArgs e)
        {
            Console.WriteLine($"kimimarubot connected!");
        }

        private void OnConnectionError(EvtConnectionErrorArgs e)
        {
            Console.WriteLine($"Failed to connect: {e.Error.Message}");
        }

        private void OnJoinedChannel(EvtJoinedChannelArgs e)
        {
            Console.WriteLine($"Joined channel \"{e.Channel}\"");
        }

        private void OnChatCommandReceived(EvtChatCommandArgs e)
        {
            
        }

        private void OnUserSentMessage(EvtUserMessageArgs e)
        {
            ProcessMsgAsInput(e);
        }

        private void OnUserMadeInput(EvtUserInputArgs e)
        {
            //InputHandler.CarryOutInput(e.ValidInputSeq.Inputs, CurConsole, ControllerMngr);

            //If auto whitelist is enabled, the user reached the whitelist message threshold,
            //the user isn't whitelisted, and the user hasn't ever been whitelisted, whitelist them
            //if (BotSettings.AutoWhitelistEnabled == true && user.Level < (int)AccessLevels.Levels.Whitelisted
            //    && user.AutoWhitelisted == false && user.ValidInputs >= BotSettings.AutoWhitelistInputCount)
            //{
            //    user.Level = (int)AccessLevels.Levels.Whitelisted;
            //    user.SetAutoWhitelist(true);
            //    if (string.IsNullOrEmpty(BotSettings.MsgSettings.AutoWhitelistMsg) == false)
            //    {
            //        //Replace the user's name with the message
            //        string msg = BotSettings.MsgSettings.AutoWhitelistMsg.Replace("{0}", user.Name);
            //        MsgHandler.QueueMessage(msg);
            //    }
            //}
        }

        private void OnWhisperReceived(EvtWhisperMessageArgs e)
        {
            
        }

        private void OnBeingHosted(EvtOnHostedArgs e)
        {
            //if (string.IsNullOrEmpty(BotSettings.MsgSettings.BeingHostedMsg) == false)
            //{
            //    string finalMsg = BotSettings.MsgSettings.BeingHostedMsg.Replace("{0}", e.HostedData.HostedByChannel);
            //    MsgHandler.QueueMessage(finalMsg);
            //}
        }

        private void OnNewSubscriber(EvtOnSubscriptionArgs e)
        {
            //if (string.IsNullOrEmpty(BotSettings.MsgSettings.NewSubscriberMsg) == false)
            //{
            //    string finalMsg = BotSettings.MsgSettings.NewSubscriberMsg.Replace("{0}", e.SubscriptionData.DisplayName);
            //    MsgHandler.QueueMessage(finalMsg);
            //}
        }

        private void OnReSubscriber(EvtOnReSubscriptionArgs e)
        {
            //if (string.IsNullOrEmpty(BotSettings.MsgSettings.ReSubscriberMsg) == false)
            //{
            //    string finalMsg = BotSettings.MsgSettings.ReSubscriberMsg.Replace("{0}", e.ReSubscriptionData.DisplayName).Replace("{1}", e.ReSubscriptionData.Months.ToString());
            //    MsgHandler.QueueMessage(finalMsg);
            //}
        }

        private void OnReconnected(EvtReconnectedArgs e)
        {
            //if (string.IsNullOrEmpty(BotSettings.MsgSettings.ReconnectedMsg) == false)
            //{
            //    MsgHandler.QueueMessage(BotSettings.MsgSettings.ReconnectedMsg);
            //}
        }

        private void OnDisconnected(EvtDisconnectedArgs e)
        {
            Console.WriteLine("Bot disconnected! Please check your internet connection.");
        }

        private void ProcessMsgAsInput(EvtUserMessageArgs e)
        {
            //User userData = e.UserData;

            //Ignore commands as inputs
            if (e.UsrMessage.Message.StartsWith('!') == true)
            {
                return;
            }

            //If there are no valid inputs, don't attempt to parse
            if (CurConsole.ValidInputs == null || CurConsole.ValidInputs.Count == 0)
            {
                return;
            }

            //Parser.InputSequence inputSequence = default;
            //(bool, List<List<Parser.Input>>, bool, int) parsedVal = default;
            InputSequence inputSequence = default;

            try
            {
                string regexStr = CurConsole.InputRegex;

                string readyMessage = InputParser.PrepParse(e.UsrMessage.Message, MacroData, SynonymData);

                //parse_message = InputParser.PopulateSynonyms(parse_message, InputGlobals.InputSynonyms);
                inputSequence = InputParser.ParseInputs(readyMessage, regexStr, new ParserOptions(0, 200, true, 60000));
                //Console.WriteLine(inputSequence.ToString());
                //Console.WriteLine("\nReverse Parsed: " + ReverseParser.ReverseParse(inputSequence));
                //Console.WriteLine("\nReverse Parsed Natural:\n" + ReverseParser.ReverseParseNatural(inputSequence));
            }
            catch (Exception exception)
            {
                string excMsg = exception.Message;

                //Kimimaru: Sanitize parsing exceptions
                //Most of these are currently caused by differences in how C# and Python handle slicing strings (Substring() vs string[:])
                //One example that throws this that shouldn't is "#mash(w234"
                //BotProgram.MsgHandler.QueueMessage($"ERROR: {excMsg}");
                inputSequence.InputValidationType = InputValidationTypes.Invalid;
                //parsedVal.Item1 = false;
            }

            //Check for non-valid messages
            if (inputSequence.InputValidationType != InputValidationTypes.Valid)
            {
                //Display error message for invalid inputs
                if (inputSequence.InputValidationType == InputValidationTypes.Invalid)
                {
                    Console.WriteLine(inputSequence.Error);
                    //BotProgram.MsgHandler.QueueMessage(inputSequence.Error);
                }

                return;
            }

            //It's a valid message, so process it
                
            //Ignore if user is silenced
            //if (userData.Silenced == true)
            //{
            //    return;
            //}

            //Ignore based on user level and permissions
            //if (userData.Level < -1)//BotProgram.BotData.InputPermissions)
            //{
            //    BotProgram.MsgHandler.QueueMessage($"Inputs are restricted to levels {(AccessLevels.Levels)BotProgram.BotData.InputPermissions} and above");
            //    return;
            //}

            #region Parser Post-Process Validation
            
            /* All this validation is very slow
             * Find a way to speed it up, ideally without integrating it directly into the parser
             */
            
            //Check if the user has permission to perform all the inputs they attempted
            //Also validate that the controller ports they're inputting for are valid
            //ParserPostProcess.InputValidation inputValidation = ParserPostProcess.CheckInputPermissionsAndPorts(userData.Level, inputSequence.Inputs,
            //    BotProgram.BotData.InputAccess.InputAccessDict);

            //If the input isn't valid, exit
            //if (inputValidation.IsValid == false)
            //{
            //    if (string.IsNullOrEmpty(inputValidation.Message) == false)
            //    {
            //        BotProgram.MsgHandler.QueueMessage(inputValidation.Message);
            //    }
            //    return;
            //}

            //Lastly, check for invalid button combos given the current console
            /*if (BotProgram.BotData.InvalidBtnCombos.InvalidCombos.TryGetValue((int)InputGlobals.CurrentConsoleVal, out List<string> invalidCombos) == true)
            {
                bool buttonCombosValidated = ParserPostProcess.ValidateButtonCombos(inputSequence.Inputs, invalidCombos);

                if (buttonCombosValidated == false)
                {
                    string msg = "Invalid input: buttons ({0}) are not allowed to be pressed at the same time.";
                    string combos = string.Empty;
                    
                    for (int i = 0; i < invalidCombos.Count; i++)
                    {
                        combos += "\"" + invalidCombos[i] + "\"";
                        
                        if (i < (invalidCombos.Count - 1))
                        {
                            combos += ", ";
                        }
                    }
                    
                    msg = string.Format(msg, combos);
                    BotProgram.MsgHandler.QueueMessage(msg);
                    
                    return;
                }
            }*/

            #endregion

            /*if (true)//InputHandler.StopRunningInputs == false)
            {
                EvtUserInputArgs userInputArgs = new EvtUserInputArgs()
                {
                    //UserData = e.UserData,
                    UsrMessage = e.UsrMessage,
                    ValidInputSeq = inputSequence
                };

                //Invoke input event
                UserMadeInputEvent?.Invoke(userInputArgs);
            }
            else
            {
                //BotProgram.MsgHandler.QueueMessage("New inputs cannot be processed until all other inputs have stopped.");
            }*/

            InputHandler.CarryOutInput(inputSequence.Inputs, CurConsole, ControllerMngr);
        }

#endregion

        private class LoginInfo
        {
            public string BotName = string.Empty;
            public string Password = string.Empty;
            public string ChannelName = string.Empty;
        }

        public class MessageSettings
        {
            /// <summary>
            /// The time, in minutes, for outputting the periodic message.
            /// </summary>
            public int MessageTime = 30;

            /// <summary>
            /// The time, in milliseconds, before each queued message will be sent.
            /// This is used as a form of rate limiting.
            /// </summary>
            public double MessageCooldown = 1000d;

            /// <summary>
            /// The message to send when the bot connects to a channel. "{0}" is replaced with the name of the bot and "{1}" is replaced with the command identifier.
            /// </summary>
            /// <para>Set empty to display no message upon connecting.</para>
            public string ConnectMessage = "{0} has connected :D ! Use {1}help to display a list of commands and {1}tutorial to see how to play! Original input parser by Jdog, aka TwitchPlays_Everything, converted to C# & improved by the community.";

            /// <summary>
            /// The message to send when the bot reconnects to chat.
            /// </summary>
            public string ReconnectedMsg = "Successfully reconnected to chat!";

            /// <summary>
            /// The message to send periodically according to <see cref="MessageTime"/>.
            /// "{0}" is replaced with the name of the bot and "{1}" is replaced with the command identifier.
            /// </summary>
            /// <para>Set empty to display no messages in the interval.</para>
            public string PeriodicMessage = "Hi! I'm {0} :D ! Use {1}help to display a list of commands!";

            /// <summary>
            /// The message to send when a user is auto whitelisted. "{0}" is replaced with the name of the user whitelisted.
            /// </summary>
            public string AutoWhitelistMsg = "{0} has been whitelisted! New commands are available.";

            /// <summary>
            /// The message to send when a new user is added to the database.
            /// "{0}" is replaced with the name of the user and "{1}" is replaced with the command identifier.
            /// </summary>
            public string NewUserMsg = "Welcome to the stream, {0} :D ! We hope you enjoy your stay!";

            /// <summary>
            /// The message to send when another channel hosts the one the bot is on.
            /// "{0}" is replaced with the name of the channel hosting the one the bot is on.
            /// </summary>
            public string BeingHostedMsg = "Thank you for hosting, {0}!!";

            /// <summary>
            /// The message to send when a user newly subscribes to the channel.
            /// "{0}" is replaced with the name of the subscriber.
            /// </summary>
            public string NewSubscriberMsg = "Thank you for subscribing, {0} :D !!";

            /// <summary>
            /// The message to send when a user resubscribes to the channel.
            /// "{0}" is replaced with the name of the subscriber and "{1}" is replaced with the number of months subscribed for.
            /// </summary>
            public string ReSubscriberMsg = "Thank you for subscribing for {1} months, {0} :D !!";
        }

        public class ClientSettings
        {
            public int ClientType = 0;
        }

        public class BingoSettings
        {
            public bool UseBingo = false;
            public string BingoPipeFilePath = string.Empty;//Globals.GetDataFilePath("BingoPipe");
        }

        public class Settings
        {
            public ClientSettings ClientSettings = null;
            public MessageSettings MsgSettings = null;
            public BingoSettings BingoSettings = null;

            /// <summary>
            /// The time, in minutes, for outputting the periodic message.
            /// </summary>
            [Obsolete("Use the value in MsgSettings instead.", false)]
            public int MessageTime = 30;
            [Obsolete("Use the value in MsgSettings instead.", false)]
            public double MessageCooldown = 1000d;
            public double CreditsTime = 2d;
            public long CreditsAmount = 100L;

            /// <summary>
            /// The character limit for bot messages. The default is the client service's character limit (Ex. Twitch).
            /// <para>Some messages that naturally go over this limit will be split into multiple messages.
            /// Examples include listing memes and macros.</para>
            /// </summary>
            public int BotMessageCharLimit = 250;//Globals.TwitchCharacterLimit;
            
            /// <summary>
            /// How long to make the main thread sleep after each iteration.
            /// Higher values use less CPU at the expense of delaying queued messages and routines.
            /// </summary>
            public int MainThreadSleep = 100;

            /// <summary>
            /// The message to send when the bot connects to a channel. "{0}" is replaced with the name of the bot and "{1}" is replaced with the command identifier.
            /// </summary>
            /// <para>Set empty to display no message upon connecting.</para>
            [Obsolete("Use the value in MsgSettings instead.", false)]
            public string ConnectMessage = "{0} has connected :D ! Use {1}help to display a list of commands and {1}tutorial to see how to play! Original input parser by Jdog, aka TwitchPlays_Everything, converted to C# & improved by the community.";

            /// <summary>
            /// The message to send periodically according to <see cref="MessageSettings.MessageTime"/>.
            /// "{0}" is replaced with the name of the bot and "{1}" is replaced with the command identifier.
            /// </summary>
            /// <para>Set empty to display no messages in the interval.</para>
            [Obsolete("Use the value in MsgSettings instead.", false)]
            public string PeriodicMessage = "Hi! I'm {0} :D ! Use {1}help to display a list of commands!";

            /// <summary>
            /// If true, automatically whitelists users if conditions are met, including the command count.
            /// </summary>
            public bool AutoWhitelistEnabled = false;

            /// <summary>
            /// The number of valid inputs required to whitelist a user if they're not whitelisted and auto whitelist is enabled.
            /// </summary>
            public int AutoWhitelistInputCount = 20;

            /// <summary>
            /// The message to send when a user is auto whitelisted. "{0}" is replaced with the name of the user whitelisted.
            /// </summary>
            [Obsolete("Use the value in MsgSettings instead.", false)]
            public string AutoWhitelistMsg = "{0} has been whitelisted! New commands are available.";
            
            /// <summary>
            /// If true, will acknowledge that a chat bot is in use and allow interacting with it, provided it's set up.
            /// </summary>
            public bool UseChatBot = false;

            /// <summary>
            /// The name of the file for the chatbot's socket in the data directory.
            /// </summary>
            public string ChatBotSocketFilename = "ChatterBotSocket";
        }
    }
}
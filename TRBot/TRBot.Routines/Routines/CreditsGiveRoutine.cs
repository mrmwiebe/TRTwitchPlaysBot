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
using TRBot.Data;

namespace TRBot.Routines
{
    /// <summary>
    /// The base class for bot routines.
    /// </summary>
    public class CreditsGiveRoutine : BaseRoutine
    {
        private DateTime CurCreditsTime;

        private readonly Dictionary<string, bool> UsersTalked = new Dictionary<string, bool>();

        public CreditsGiveRoutine()
        {
            Identifier = "creditsgive";
        }

        public override void Initialize(DataContainer dataContainer)
        {
            base.Initialize(dataContainer);

            DataContainer.MessageHandler.ClientService.EventHandler.UserSentMessageEvent -= MessageReceived;
            DataContainer.MessageHandler.ClientService.EventHandler.UserSentMessageEvent += MessageReceived;

            CurCreditsTime = DateTime.UtcNow;
        }

        public override void CleanUp()
        {
            base.CleanUp();

            DataContainer.MessageHandler.ClientService.EventHandler.UserSentMessageEvent -= MessageReceived;

            DataContainer = null;
        }

        public override void UpdateRoutine(in DateTime currentTimeUTC)
        {
            TimeSpan creditsDiff = currentTimeUTC - CurCreditsTime;

            using BotDBContext context = DatabaseManager.OpenContext();

            long creditsTimeMS = DataHelper.GetSettingIntNoOpen(SettingsConstants.CREDITS_GIVE_TIME, context, -1L);

            //Don't do anything if the credits time is less than 0
            if (creditsTimeMS < 0L)
            {
                CurCreditsTime = currentTimeUTC;
                return;
            }

            long creditsGiveAmount = DataHelper.GetSettingIntNoOpen(SettingsConstants.CREDITS_GIVE_AMOUNT, context, 100L);

            //Check if we surpassed the time
            if (creditsDiff.TotalMilliseconds >= creditsTimeMS)
            {
                string[] talkedNames = UsersTalked.Keys.ToArray();
                for (int i = 0; i < talkedNames.Length; i++)
                {
                    User user = DataHelper.GetUserNoOpen(talkedNames[i], context);
                    user.Stats.Credits += creditsGiveAmount;

                    //Console.WriteLine($"Gave {user.Name} credits!");
                }

                context.SaveChanges();
                
                UsersTalked.Clear();

                CurCreditsTime = currentTimeUTC;
            }
        }

        private void MessageReceived(EvtUserMessageArgs e)
        {
            string nameToLower = e.UsrMessage.Username.ToLowerInvariant();

            //Check if the user talked before
            if (UsersTalked.ContainsKey(nameToLower) == false)
            {
                //If so, check if they're in the database and not opted out, then add them for gaining credits
                using (BotDBContext context = DatabaseManager.OpenContext())
                {
                    long creditsTimeMS = DataHelper.GetSettingIntNoOpen(SettingsConstants.CREDITS_GIVE_TIME, context, -1L);

                    //Don't do anything if the credits time is less than 0
                    if (creditsTimeMS < 0L)
                    {
                        return;
                    }

                    User user = DataHelper.GetUserNoOpen(nameToLower, context);
                    if (user != null && user.IsOptedOut == false)
                    {
                        UsersTalked.Add(nameToLower, true);
                    }
                }
            }
        }
    }
}
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
using System.Text;
using System.IO;
using System.Linq;
using TRBot.Permissions;
using TRBot.Connection;
using TRBot.Consoles;
using TRBot.Utilities;

namespace TRBot.Data
{
    /// <summary>
    /// Helps retrieve data from the database.
    /// </summary>
    public static class DataHelper
    {
        /// <summary>
        /// Obtains a setting from the database.
        /// </summary>        
        /// <param name="settingName">The name of the setting to retrieve.</param>
        /// <returns>A Settings object corresponding to settingName. If the setting is not found, null.</returns>
        public static Settings GetSetting(string settingName)
        {
            using (BotDBContext dbContext = DatabaseManager.OpenContext())
            {
                return dbContext.SettingCollection.FirstOrDefault((set) => set.Key == settingName);
            }
        }

        /// <summary>
        /// Obtains a setting from the database with an opened context.
        /// </summary>        
        /// <param name="settingName">The name of the setting to retrieve.</param>
        /// <param name="context">The open database context.</param>
        /// <returns>A Settings object corresponding to settingName. If the setting is not found, null.</returns>
        public static Settings GetSettingNoOpen(string settingName, BotDBContext context)
        {
            return context.SettingCollection.FirstOrDefault((set) => set.Key == settingName);
        }

        /// <summary>
        /// Obtains a setting integer value from the database.
        /// </summary>        
        /// <param name="settingName">The name of the setting to retrieve.</param>
        /// <param name="defaultVal">The default value to fallback to if not found.</param>
        /// <returns>An integer for settingName. If the setting is not found, the default value.</returns>
        public static long GetSettingInt(string settingName, in long defaultVal)
        {
            Settings setting = GetSetting(settingName);

            return setting != null ? setting.ValueInt : defaultVal;
        }

        /// <summary>
        /// Obtains a setting integer value from the database with an opened context.
        /// </summary>        
        /// <param name="settingName">The name of the setting to retrieve.</param>
        /// <param name="context">The open database context.</param>
        /// <param name="defaultVal">The default value to fallback to if not found.</param>
        /// <returns>An integer for settingName. If the setting is not found, the default value.</returns>
        public static long GetSettingIntNoOpen(string settingName, BotDBContext context, in long defaultVal)
        {
            Settings setting = GetSettingNoOpen(settingName, context);

            return setting != null ? setting.ValueInt : defaultVal;
        }

        /// <summary>
        /// Obtains a setting string value from the database.
        /// </summary>        
        /// <param name="settingName">The name of the setting to retrieve.</param>
        /// <param name="defaultVal">The default value to fallback to if not found.</param>
        /// <returns>A string value for settingName. If the setting is not found, the default value.</returns>
        public static string GetSettingString(string settingName, string defaultVal)
        {
            Settings setting = GetSetting(settingName);

            return setting != null ? setting.ValueStr : defaultVal;
        }

        /// <summary>
        /// Obtains a setting string value from the database with an opened context.
        /// </summary>        
        /// <param name="settingName">The name of the setting to retrieve.</param>
        /// <param name="context">The open database context.</param>
        /// <param name="defaultVal">The default value to fallback to if not found.</param>
        /// <returns>A string value for settingName. If the setting is not found, the default value.</returns>
        public static string GetSettingStringNoOpen(string settingName, BotDBContext context, string defaultVal)
        {
            Settings setting = GetSettingNoOpen(settingName, context);

            return setting != null ? setting.ValueStr : defaultVal;
        }

        /// <summary>
        /// A helper method to obtain the name of the bot credits.
        /// </summary>        
        /// <returns>The name of the bot credits.</returns>
        public static string GetCreditsName()
        {
            return GetSettingString(SettingsConstants.CREDITS_NAME, "Credits");
        }

        /// <summary>
        /// A helper method to obtain the name of the bot credits with an opened context.
        /// </summary>
        /// <param name="context">The open database context.</param>
        /// <returns>The name of the bot credits.</returns>
        public static string GetCreditsNameNoOpen(BotDBContext context)
        {
            return GetSettingStringNoOpen(SettingsConstants.CREDITS_NAME, context, "Credits");
        }

        /// <summary>
        /// A helper method to obtain the current client service type.
        /// </summary>
        /// <returns>The current ClientServiceType.</returns>
        public static ClientServiceTypes GetClientServiceType()
        {
            return (ClientServiceTypes)GetSettingInt(SettingsConstants.CLIENT_SERVICE_TYPE, 0L);
        }

        /// <summary>
        /// A helper method to obtain the current client service type with an opened context.
        /// </summary>
        /// <param name="context">The open database context.</param>
        /// <returns>The current ClientServiceType.</returns>
        public static ClientServiceTypes GetClientServiceTypeNoOpen(BotDBContext context)
        {
            return (ClientServiceTypes)GetSettingInt(SettingsConstants.CLIENT_SERVICE_TYPE, 0L);
        }

        /// <summary>
        /// Obtains a user object from the database with an opened context.
        /// </summary>
        /// <param name="userName">The name of the user.</param>
        /// <param name="context">The open database context.</param>
        /// <returns>A user object with the given userName. null if not found.</returns>
        public static User GetUserNoOpen(string userName, BotDBContext context)
        {
            string userNameLowered = userName.ToLowerInvariant();

            return context.Users.FirstOrDefault(u => u.Name == userNameLowered);
        }

        /// <summary>
        /// Obtains a user object from the database with an opened context.
        //  If it doesn't exist, a new one will be added to the database.
        /// </summary>        
        /// <param name="userName">The name of the user.</param>
        /// <param name="context">The open database context.</param>
        /// <param name="added">Whether a new user was added to the database.</param>
        /// <returns>A user object with the given userName.</returns>
        public static User GetOrAddUserNoOpen(string userName, BotDBContext context, out bool added)
        {
            //Add the lowered version of their name to simplify retrieval
            string userNameLowered = userName.ToLowerInvariant();

            User user = context.Users.FirstOrDefault(u => u.Name == userNameLowered);
            
            added = false;

            //If the user doesn't exist, add it
            if (user == null)
            {
                long controllerPort = 0L;

                //Check which port to set if teams mode is enabled
                long teamsModeEnabled = GetSettingIntNoOpen(SettingsConstants.TEAMS_MODE_ENABLED, context, 0L);
                if (teamsModeEnabled > 0L)
                {
                    Settings teamsNextPort = GetSettingNoOpen(SettingsConstants.TEAMS_MODE_NEXT_PORT, context);
                    
                    //The player is now on this port
                    controllerPort = teamsNextPort.ValueInt;

                    long maxPort = GetSettingIntNoOpen(SettingsConstants.TEAMS_MODE_MAX_PORT, context, 3L);

                    //Increment the next port value, keeping it in range
                    teamsNextPort.ValueInt = Utilities.Helpers.Wrap(teamsNextPort.ValueInt + 1, 0L, maxPort + 1);
                }

                //Give them User permissions and set their port
                user = new User(userNameLowered, (long)PermissionLevels.User);
                user.ControllerPort = controllerPort;

                context.Users.Add(user);

                //Save the changes so the user object is in the database
                context.SaveChanges();

                //Update this user's abilities off the bat
                UpdateUserAutoGrantAbilities(user, context);

                //Save changes again to update the abilities
                context.SaveChanges();

                added = true;
            }

            return user;
        }

        /// <summary>
        /// Retrieves a user's overridden default input duration, or if they don't have one, the global default input duration.
        /// </summary>
        /// <param name="user">The User object.</param>
        /// <param name="context">The open database context</param>
        /// <returns>The user-overridden or global default input duration.</returns>
        public static long GetUserOrGlobalDefaultInputDur(User user, BotDBContext context)
        {
            //Check for a user-overridden default input duration
            if (user != null && user.TryGetAbility(PermissionConstants.USER_DEFAULT_INPUT_DIR_ABILITY, out UserAbility defaultDurAbility) == true
                && defaultDurAbility.IsEnabled == true)
            {
                return defaultDurAbility.ValueInt;
            }
            //Use global max input duration
            else
            {
                return DataHelper.GetSettingIntNoOpen(SettingsConstants.DEFAULT_INPUT_DURATION, context, 200L);
            }
        }

        /// <summary>
        /// Retrieves a user's overridden max input duration, or if they don't have one, the global max input duration.
        /// </summary>
        /// <param name="user">The User object.</param>
        /// <param name="context">The open database context</param>
        /// <returns>The user-overridden or global max input duration.</returns>
        public static long GetUserOrGlobalMaxInputDur(User user, BotDBContext context)
        {
            //Check for a user-overridden max input duration
            if (user != null && user.TryGetAbility(PermissionConstants.USER_MAX_INPUT_DIR_ABILITY, out UserAbility maxDurAbility) == true
                && maxDurAbility.IsEnabled == true)
            {
                return maxDurAbility.ValueInt;
            }
            //Use global max input duration
            else
            {
                return DataHelper.GetSettingIntNoOpen(SettingsConstants.MAX_INPUT_DURATION, context, 60000L);
            }
        }

        /// <summary>
        /// Fully updates a user's available abilities based on their current level.
        /// </summary>
        /// <param name="user">The User object to update the abilities on.</param>
        /// <param name="newLevel">The new level the user will be set to.</param>
        /// <param name="context">The open database context.</param>
        public static void UpdateUserAutoGrantAbilities(User user, BotDBContext context)
        {
            long originalLevel = user.Level;

            //First, disable all auto grant abilities the user has
            //Don't disable abilities that were given by a higher level
            //This prevents users from removing constraints imposed by moderators and such
            IEnumerable<UserAbility> abilities = user.UserAbilities.Where(p => (long)p.PermAbility.AutoGrantOnLevel >= 0
                    && p.GrantedByLevel <= originalLevel);

            foreach (UserAbility ability in abilities)
            {
                ability.SetEnabledState(false);
                ability.Expiration = null;
                ability.GrantedByLevel = -1;
            }

            //Get all auto grant abilities up to the user's level
            IEnumerable<PermissionAbility> permAbilities =
                context.PermAbilities.Where(p => (long)p.AutoGrantOnLevel >= 0
                    && (long)p.AutoGrantOnLevel <= originalLevel);

            Console.WriteLine($"Found {permAbilities.Count()} autogrant up to level {originalLevel}");

            //Enable all of those abilities
            foreach (PermissionAbility permAbility in permAbilities)
            {
                user.EnableAbility(permAbility);
            }
        }

        /// <summary>
        /// Updates user abilities upon changing the user's level.
        /// </summary>
        /// <param name="user">The User object to adjust the abilities on.</param>
        /// <param name="newLevel">The new level the user will be set to.</param>
        /// <param name="context">The open database context.</param>
        public static void AdjustUserAbilitiesOnLevel(User user, long newLevel, BotDBContext context)
        {
            long originalLevel = user.Level;

            //Nothing to do here if the levels are the same
            if (originalLevel == newLevel)
            {
                return;
            }

            //Disable all abilities down to the new level
            if (originalLevel > newLevel)
            {
                //Look for all auto grant abilities that are less than or equal to the original level
                //and greater than the new level, and disable them
                IEnumerable<UserAbility> abilities = user.UserAbilities.Where(p => p.PermAbility.AutoGrantOnLevel >= 0
                    && (long)p.PermAbility.AutoGrantOnLevel <= originalLevel
                    && (long)p.PermAbility.AutoGrantOnLevel > newLevel);

                foreach (UserAbility ability in abilities)
                {
                    ability.SetEnabledState(false);
                    ability.Expiration = null;
                    ability.GrantedByLevel = -1;
                }
            }
            //Enable all abilities up to the new level
            else if (originalLevel < newLevel)
            {
                //Look for all auto grant abilities that are greater than the original level
                //and less than or equal to the new level
                IEnumerable<PermissionAbility> permAbilities =
                    context.PermAbilities.Where(p => (long)p.AutoGrantOnLevel >= 0
                        && (long)p.AutoGrantOnLevel > originalLevel && (long)p.AutoGrantOnLevel <= newLevel);

                //Add all these abilities
                foreach (PermissionAbility pAbility in permAbilities)
                {
                    user.EnableAbility(pAbility);
                }
            }
        }

        /// <summary>
        /// Initializes default values for data into a given context.
        /// </summary>
        /// <param name="dbContext">The opened context to initialize default values for.</param>
        /// <returns>An int representing the number of entries added into the context.</returns>
        public static int InitDefaultData(BotDBContext dbContext)
        {
            int entriesAdded = 0;

            /* First check if we should actually initialize defaults
             * This depends on the force init setting: initialize defaults if it's either missing or true
             * If the data version is less than the bot version, then we set force init to true
             */

            //Check data version
            Settings dataVersionSetting = DataHelper.GetSettingNoOpen(SettingsConstants.DATA_VERSION_NUM, dbContext);
                
            //Add the version to the lowest number if the entry doesn't exist
            //This will force an init
            if (dataVersionSetting == null)
            {
                dataVersionSetting = new Settings(SettingsConstants.DATA_VERSION_NUM, "0.0.0", 0L);
                dbContext.SettingCollection.Add(dataVersionSetting);
                    
                entriesAdded++;
                Console.WriteLine($"Data version setting \"{SettingsConstants.DATA_VERSION_NUM}\" not found in database; adding.");
            }
                
            string dataVersionStr = dataVersionSetting.ValueStr;

            //Compare versions
            Version dataVersion = new Version(dataVersionStr);
            Version curVersion = new Version(Application.VERSION_NUMBER);

            int result = dataVersion.CompareTo(curVersion);

            Settings forceInitSetting = DataHelper.GetSettingNoOpen(SettingsConstants.FORCE_INIT_DEFAULTS, dbContext);
            if (forceInitSetting == null)
            {
                forceInitSetting = new Settings(SettingsConstants.FORCE_INIT_DEFAULTS, string.Empty, 1L);
                dbContext.SettingCollection.Add(forceInitSetting);

                entriesAdded++;
                Console.WriteLine($"Force initialize setting \"{SettingsConstants.FORCE_INIT_DEFAULTS}\" not found in database; adding.");
            }

            long forceInit = forceInitSetting.ValueInt;

            //The bot version is greater, so update the data version number and set it to force init
            if (result < 0)
            {
                Console.WriteLine($"Data version {dataVersionSetting.ValueStr} is less than bot version {Application.VERSION_NUMBER}. Updating version number and forcing database initialization for missing entries.");
                dataVersionSetting.ValueStr = Application.VERSION_NUMBER;
            }
            //If the data version is greater than the bot, we should let them know
            else if (result > 0)
            {
                Console.WriteLine($"Data version {dataVersionSetting.ValueStr} is greater than bot version {Application.VERSION_NUMBER}. Ensure you're running the correct version of TRBot to avoid potential issues.");
            }

            //Initialize if we're told to
            if (forceInit > 0)
            {
                Console.WriteLine($"{SettingsConstants.FORCE_INIT_DEFAULTS} is true; initializing missing defaults in database.");

                //Tell it to no longer force initializing
                forceInitSetting.ValueInt = 0;

                //Check all settings with the defaults
                List<Settings> settings = DefaultData.GetDefaultSettings();
                for (int i = 0; i < settings.Count; i++)
                {
                    Settings setting = settings[i];
                        
                    //See if the setting exists
                    Settings foundSetting = dbContext.SettingCollection.FirstOrDefault((set) => set.Key == setting.Key);
                        
                    if (foundSetting == null)
                    {
                        //Default setting does not exist, so add it
                        dbContext.SettingCollection.Add(setting);
                        entriesAdded++;
                    }
                }

                List<CommandData> cmdData = DefaultData.GetDefaultCommands();
                for (int i = 0; i < cmdData.Count; i++)
                {
                    CommandData commandData = cmdData[i];
                        
                    //See if the command data exists
                    CommandData foundCommand = dbContext.Commands.FirstOrDefault((cmd) => cmd.Name == commandData.Name);
                        
                    if (foundCommand == null)
                    {
                        //Default command does not exist, so add it
                        dbContext.Commands.Add(commandData);
                        entriesAdded++;
                    }
                 }

                List<PermissionAbility> permAbilities = DefaultData.GetDefaultPermAbilities();
                for (int i = 0; i < permAbilities.Count; i++)
                {
                    PermissionAbility permAbility = permAbilities[i];
                        
                    //See if the command data exists
                    PermissionAbility foundPerm = dbContext.PermAbilities.FirstOrDefault((pAb) => pAb.Name == permAbility.Name);
                        
                    if (foundPerm == null)
                    {
                        //Default permission ability does not exist, so add it
                        dbContext.PermAbilities.Add(permAbility);
                        entriesAdded++;
                    }
                }

                Settings firstLaunchSetting = DataHelper.GetSettingNoOpen(SettingsConstants.FIRST_LAUNCH, dbContext);
                if (firstLaunchSetting == null)
                {
                    firstLaunchSetting = new Settings(SettingsConstants.FIRST_LAUNCH, string.Empty, 1L);
                    dbContext.SettingCollection.Add(firstLaunchSetting);

                    entriesAdded++;
                }

                //Do these things upon first launching the bot
                if (firstLaunchSetting.ValueInt > 0)
                {
                    //Populate default consoles - this will also populate inputs
                    List<GameConsole> consoleData = DefaultData.GetDefaultConsoles();
                    if (dbContext.Consoles.Count() < consoleData.Count)
                    {
                        for (int i = 0; i < consoleData.Count; i++)
                        {
                            GameConsole console = consoleData[i];
                            
                            //See if the console exists
                            GameConsole foundConsole = dbContext.Consoles.FirstOrDefault((c) => c.Name == console.Name);
                            if (foundConsole == null)
                            {
                                //This console isn't in the database, so add it
                                dbContext.Consoles.Add(console);

                                entriesAdded++;
                            }
                        }
                    }

                    //Set first launch to 0
                    firstLaunchSetting.ValueInt = 0;
                }
            }

            return entriesAdded;
        }
    }
}

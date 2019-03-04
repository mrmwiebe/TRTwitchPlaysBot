﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;

namespace KimimaruBot
{
    public sealed class HighestCreditsCommand : BaseCommand
    {
        public HighestCreditsCommand()
        {

        }

        public override void ExecuteCommand(object sender, OnChatCommandReceivedArgs e)
        {
            if (CreditsCommand.UserCredits == null || CreditsCommand.UserCredits.Count == 0)
            {
                BotProgram.QueueMessage("Sorry, the credits database is missing or empty!");
                return;
            }

            List<string> highestCreditsUsers = new List<string>();

            //Copy since commands are handled in another thread
            //If looping with foreach, the credits dictionary can be modified, which will throw an exception
            KeyValuePair<string, long>[] dict = CreditsCommand.UserCredits.ToArray();

            long highestCredits = -1L;

            for (int i = 0; i < dict.Length; i++)
            {
                if (dict[i].Value > highestCredits)
                {
                    highestCredits = dict[i].Value;
                }
            }

            for (int i = 0; i < dict.Length; i++)
            {
                if (dict[i].Value == highestCredits)
                {
                    highestCreditsUsers.Add(dict[i].Key);
                }
            }

            string users = string.Empty;

            for (int i = 0; i < highestCreditsUsers.Count; i++)
            {
                users += highestCreditsUsers[i];

                int indp1 = i + 1;

                if (i < (highestCreditsUsers.Count - 1))
                {
                    users += ", ";
                    if (indp1 == (highestCreditsUsers.Count - 1))
                    {
                        users += "and ";
                    }
                }
            }

            if (highestCreditsUsers.Count == 1)
            {
                users += " has";
            }
            else
            {
                users += " have";
            }

            BotProgram.QueueMessage($"{users} the most number of credits with a credit total of {highestCredits}!");
        }
    }
}

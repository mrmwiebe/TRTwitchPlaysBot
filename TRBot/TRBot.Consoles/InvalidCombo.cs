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

namespace TRBot.Consoles
{
    /// <summary>
    /// Represents an input in an invalid input combo.
    /// </summary> 
    public class InvalidCombo
    {
        /// <summary>
        /// The ID of the invalid combo.
        /// </summary>
        public int ID { get; set; } = 0;

        /// <summary>
        /// The ID of the input.
        /// </summary>
        public int InputID { get; set; } = 0;

        /// <summary>
        /// The InputData associated with this invalid combo.
        /// This is used by the database and should not be assigned or modified manually.
        /// </summary>
        public virtual InputData Input { get; set; } = null;

        public InvalidCombo()
        {

        }

        public InvalidCombo(InputData inputData)
        {
            Input = inputData;
        }
    }
}

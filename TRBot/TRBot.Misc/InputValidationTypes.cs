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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRBot.Misc
{
    /// <summary>
    /// The types of input validation.
    /// </summary>
    public enum InputValidationTypes
    {
        Valid = 0,
        RestrictedInputs = 1,
        InvalidCombo = 2,
        OverMaxDuration = 3,
        InsufficientAccess = 4,
        InvalidPort = 5,
        Other = 6
    }
}

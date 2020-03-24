/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Diagnostics;

namespace eduVPN.Models
{
    public static class Extensions
    {
        /// <summary>
        /// Returns localized <see cref="InstanceSourceType"/> enum title
        /// </summary>
        /// <param name="value"><see cref="InstanceSourceType"/> enum</param>
        /// <returns>Localized <see cref="InstanceSourceType"/> enum title</returns>
        [DebuggerStepThrough]
        public static string GetLocalizableName(this InstanceSourceType value)
        {
            switch (value)
            {
                case InstanceSourceType.SecureInternet: return Resources.Strings.InstanceSourceTypeSecureInternet;
                case InstanceSourceType.InstituteAccess: return Resources.Strings.InstanceSourceTypeInstituteAccess;
                default: return Enum.GetName(typeof(InstanceSourceType), value);
            }
        }
    }
}

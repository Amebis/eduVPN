/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Configuration;
using System.Diagnostics;

namespace eduVPN.Properties
{
    public sealed partial class Settings : ApplicationSettingsBase
    {
        [UserScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue("")]
        [Obsolete("Please use OpenVPNInterfaceID instead")]
        [NoSettingsVersionUpgrade]
        public string OpenVPNInterface
        {
            get { throw new NotSupportedException("OpenVPNInterface is obsolete"); }
            set { throw new NotSupportedException("OpenVPNInterface is obsolete"); }
        }
    }
}

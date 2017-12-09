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

        [UserScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue("")]
        [Obsolete("Please use SecureInternetInstanceSourceInfo instead")]
        [NoSettingsVersionUpgrade]
        public Xml.VPNConfigurationSettingsList SecureInternetConfigHistory
        {
            get { throw new NotSupportedException("SecureInternetConfigHistory is obsolete"); }
            set { throw new NotSupportedException("SecureInternetConfigHistory is obsolete"); }
        }

        [UserScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue("")]
        [Obsolete("Please use InstituteAccessInstanceSourceInfo instead")]
        [NoSettingsVersionUpgrade]
        public Xml.VPNConfigurationSettingsList InstituteAccessConfigHistory
        {
            get { throw new NotSupportedException("InstituteAccessConfigHistory is obsolete"); }
            set { throw new NotSupportedException("InstituteAccessConfigHistory is obsolete"); }
        }

        [UserScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue("<SerializableStringDictionary />")]
        [Obsolete("Please use AccessTokenCache instead")]
        [NoSettingsVersionUpgrade]
        public Xml.SerializableStringDictionary AccessTokens
        {
            get { throw new NotSupportedException("AccessTokens is obsolete"); }
            set { throw new NotSupportedException("AccessTokens is obsolete"); }
        }
    }
}

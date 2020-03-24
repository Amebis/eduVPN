/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Specialized;
using System.Configuration;

namespace eduVPN.Client.Properties
{
    /// <summary>
    /// eduVPN client settings
    /// </summary>
    public sealed partial class Settings : ApplicationSettingsBase
    {
        #region Properties

        /// <summary>
        /// Client window top coordinate
        /// </summary>
        [UserScopedSetting()]
        [SettingsDescription("Client window top coordinate")]
        [DefaultSettingValue("NaN")]
        [Obsolete("Please use eduVPN.Views.Properties.Settings.Default.WindowTop instead")]
        [NoSettingsVersionUpgrade]
        public double WindowTop
        {
            get { throw new NotSupportedException("WindowTop is obsolete"); }
            set { throw new NotSupportedException("WindowTop is obsolete"); }
        }

        /// <summary>
        /// Client window left coordinate
        /// </summary>
        [UserScopedSetting()]
        [SettingsDescription("Client window left coordinate")]
        [DefaultSettingValue("NaN")]
        [Obsolete("Please use eduVPN.Views.Properties.Settings.Default.WindowLeft instead")]
        [NoSettingsVersionUpgrade]
        public double WindowLeft
        {
            get { throw new NotSupportedException("WindowLeft is obsolete"); }
            set { throw new NotSupportedException("WindowLeft is obsolete"); }
        }

        /// <summary>
        /// Recently used hostnames for custom instance connections
        /// </summary>
        [UserScopedSetting()]
        [SettingsDescription("Recently used hostnames for custom instance connections")]
        [Obsolete("Please use eduVPN.Views.Properties.Settings.Default.CustomInstanceHistory instead")]
        [NoSettingsVersionUpgrade]
        public StringCollection CustomInstanceHistory
        {
            get { throw new NotSupportedException("CustomInstanceHistory is obsolete"); }
            set { throw new NotSupportedException("CustomInstanceHistory is obsolete"); }
        }

        /// <summary>
        /// Has user been informed that the client minimizes to the system tray already?
        /// </summary>
        [UserScopedSetting()]
        [SettingsDescription("Has user been informed that the client minimizes to the system tray already?")]
        [DefaultSettingValue("False")]
        [Obsolete("Please use eduVPN.Views.Properties.Settings.Default.SystemTrayMinimizedWarned instead")]
        [NoSettingsVersionUpgrade]
        public bool SystemTrayMinimizedWarned
        {
            get { throw new NotSupportedException("SystemTrayMinimizedWarned is obsolete"); }
            set { throw new NotSupportedException("SystemTrayMinimizedWarned is obsolete"); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize settings
        /// </summary>
        public static void Initialize()
        {
            if (Default.SettingsVersion == 0)
            {
                // Migrate settings from previous version.
                Default.Upgrade();
                Default.SettingsVersion = 1;

                // Versions before 1.0.19 stored user settings here, then we moved them to eduVPN.Views.
                if (Default.GetPreviousVersion("WindowTop") is double window_top)
                    Views.Properties.Settings.Default.WindowTop = window_top;
                if (Default.GetPreviousVersion("WindowLeft") is double window_left)
                    Views.Properties.Settings.Default.WindowLeft = window_left;
                if (Default.GetPreviousVersion("CustomInstanceHistory") is StringCollection custom_instance_history)
                    Views.Properties.Settings.Default.CustomInstanceHistory = custom_instance_history;
                if (Default.GetPreviousVersion("SystemTrayMinimizedWarned") is bool system_tray_minimized_warned)
                    Views.Properties.Settings.Default.SystemTrayMinimizedWarned = system_tray_minimized_warned;
            }
        }

        #endregion
    }
}

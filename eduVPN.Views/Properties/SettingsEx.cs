/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Interop;

namespace eduVPN.Views.Properties
{
    /// <summary>
    /// Settings wrapper to support configuration overriding using registry
    /// </summary>
    public class SettingsEx : eduVPN.Properties.ApplicationSettingsBaseEx
    {
        #region Properties

        /// <summary>
        /// Default settings
        /// </summary>
        public static SettingsEx Default { get; } = ((SettingsEx)Synchronized(new SettingsEx()));

        /// <see cref="Settings.ProcessRenderMode"/>
        public RenderMode ProcessRenderMode
        {
            get
            {
                if (GetValue(nameof(ProcessRenderMode), out uint value))
                    return (RenderMode)value;
                return Settings.Default.ProcessRenderMode;
            }
        }

        /// <summary>
        /// Start application on user sign-on
        /// </summary>
        public bool StartOnSignon
        {
            get
            {
                if (GetValue(nameof(StartOnSignon), out bool value))
                    return value;
                return Settings.Default.StartOnSignon;
            }
            set
            {
                if (IsStartOnSignonModifiable)
                    Settings.Default.StartOnSignon = value;
            }
        }

        /// <summary>
        /// May user change StartOnSignon?
        /// </summary>
        public bool IsStartOnSignonModifiable
        {
            get => !GetValue(nameof(StartOnSignon), out bool value);
        }

        #endregion
    }
}

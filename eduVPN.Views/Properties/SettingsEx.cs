/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
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

        #endregion
    }
}

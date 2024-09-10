/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.VPN;
using eduVPN.Xml;
using System;
using System.Collections.Specialized;

namespace eduVPN.Properties
{
    /// <summary>
    /// Settings wrapper to support configuration overriding using registry
    /// </summary>
    public class SettingsEx : ApplicationSettingsBaseEx
    {
        #region Properties

        /// <summary>
        /// Default settings
        /// </summary>
        public static SettingsEx Default { get; } = (SettingsEx)Synchronized(new SettingsEx());

        /// <see cref="Settings.OpenVPNInteractiveServiceInstance"/>
        public string OpenVPNInteractiveServiceInstance => GetValue(nameof(OpenVPNInteractiveServiceInstance), out string value) ? value : Settings.Default.OpenVPNInteractiveServiceInstance;

        /// <see cref="Settings.OpenVPNRemoveOptions"/>
        public StringCollection OpenVPNRemoveOptions => GetValue(nameof(OpenVPNRemoveOptions), out StringCollection value) ? value : Settings.Default.OpenVPNRemoveOptions;

        /// <see cref="Settings.OpenVPNAddOptions"/>
        public string OpenVPNAddOptions => GetValue(nameof(OpenVPNAddOptions), out string[] value) ? string.Join(Environment.NewLine, value) : Settings.Default.OpenVPNAddOptions;

        /// <see cref="Settings.WireGuardKillSwitch"/>
        public WireGuardKillSwitchMode WireGuardKillSwitch => GetValue(nameof(WireGuardKillSwitch), out uint value) ? (WireGuardKillSwitchMode)value : Settings.Default.WireGuardKillSwitch;

        /// <see cref="Settings.SelfUpdateDiscovery"/>
        public ResourceRef SelfUpdateDiscovery => GetValue(nameof(SelfUpdateDiscovery), out ResourceRef value) ? value : Settings.Default.SelfUpdateDiscovery;

        /// <summary>
        /// List of preconfigured institute access servers
        /// </summary>
        public UriList InstituteAccessServers => GetValue(nameof(InstituteAccessServers), out UriList value) ? value : null;

        /// <summary>
        /// Preconfigured Secure Internet organization Id
        /// </summary>
        public string SecureInternetOrganization => GetValue(nameof(SecureInternetOrganization), out string value) ? value : null;

        #endregion
    }
}

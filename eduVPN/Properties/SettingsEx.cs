/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

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
        public static SettingsEx Default { get; } = ((SettingsEx)Synchronized(new SettingsEx()));

        /// <see cref="Settings.AcceptProfileTypes"/>
        public StringCollection AcceptProfileTypes
        {
            get
            {
                if (GetValue(nameof(AcceptProfileTypes), out StringCollection value))
                    return value;
                return Settings.Default.AcceptProfileTypes;
            }
        }

        /// <see cref="Settings.OpenVPNInteractiveServiceInstance"/>
        public string OpenVPNInteractiveServiceInstance
        {
            get
            {
                if (GetValue(nameof(OpenVPNInteractiveServiceInstance), out string value))
                    return value;
                return Settings.Default.OpenVPNInteractiveServiceInstance;
            }
        }

        /// <see cref="Settings.OpenVPNRemoveOptions"/>
        public StringCollection OpenVPNRemoveOptions
        {
            get
            {
                if (GetValue(nameof(OpenVPNRemoveOptions), out StringCollection value))
                    return value;
                return Settings.Default.OpenVPNRemoveOptions;
            }
        }

        /// <see cref="Settings.OpenVPNAddOptions"/>
        public string OpenVPNAddOptions
        {
            get
            {
                if (GetValue(nameof(OpenVPNAddOptions), out string[] value))
                    return string.Join(Environment.NewLine, value);
                return Settings.Default.OpenVPNAddOptions;
            }
        }

        /// <see cref="Settings.ServersDiscovery"/>
        public ResourceRef ServersDiscovery
        {
            get
            {
                if (GetValue(nameof(ServersDiscovery), out ResourceRef value))
                    return value;
                return Settings.Default.ServersDiscovery;
            }
        }

        /// <see cref="Settings.OrganizationsDiscovery"/>
        public ResourceRef OrganizationsDiscovery
        {
            get
            {
                if (GetValue(nameof(OrganizationsDiscovery), out ResourceRef value))
                    return value;
                return Settings.Default.OrganizationsDiscovery;
            }
        }

        /// <see cref="Settings.SelfUpdateDiscovery"/>
        public ResourceRef SelfUpdateDiscovery
        {
            get
            {
                if (GetValue(nameof(SelfUpdateDiscovery), out ResourceRef value))
                    return value;
                return Settings.Default.SelfUpdateDiscovery;
            }
        }

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

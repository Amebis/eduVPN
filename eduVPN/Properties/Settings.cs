/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using System;
using System.ComponentModel;
using System.Configuration;

namespace eduVPN.Properties
{
    /// <summary>
    /// eduVPN settings
    /// </summary>
    public sealed partial class Settings : ApplicationSettingsBase
    {
        #region Properties

        /// <summary>
        /// Was client started at user sign-on?
        /// </summary>
        public bool IsSignon { get; set; }

        /// <summary>
        /// Institute access settings (v1)
        /// </summary>
        [UserScopedSetting()]
        [Obsolete]
        [NoSettingsVersionUpgrade]
        public Xml.InstanceSourceSettings InstituteAccessInstanceSourceSettings
        {
            get { throw new NotSupportedException("InstituteAccessInstanceSourceSettings is obsolete"); }
            set { throw new NotSupportedException("InstituteAccessInstanceSourceSettings is obsolete"); }
        }

        /// <summary>
        /// Profile to automatically connect to on startup
        /// </summary>
        [UserScopedSetting()]
        [Obsolete]
        [NoSettingsVersionUpgrade]
        public Xml.StartSessionParams AutoStartProfile
        {
            get { throw new NotSupportedException("AutoStartProfile is obsolete"); }
            set { throw new NotSupportedException("AutoStartProfile is obsolete"); }
        }

        /// <summary>
        /// Always connect using TCP.
        /// </summary>
        [UserScopedSetting()]
        [Obsolete]
        [NoSettingsVersionUpgrade]
        public bool OpenVPNForceTCP
        {
            get { throw new NotSupportedException("OpenVPNForceTCP is obsolete"); }
            set { throw new NotSupportedException("OpenVPNForceTCP is obsolete"); }
        }

        /// <summary>
        /// Prefer connecting using TCP.
        /// </summary>
        [UserScopedSetting()]
        [Obsolete]
        [NoSettingsVersionUpgrade]
        public bool OpenVPNPreferTCP
        {
            get { throw new NotSupportedException("OpenVPNPreferTCP is obsolete"); }
            set { throw new NotSupportedException("OpenVPNPreferTCP is obsolete"); }
        }

        /// <summary>
        /// List of institute access servers user connects to
        /// </summary>
        [UserScopedSetting()]
        [Obsolete]
        [NoSettingsVersionUpgrade]
        public Xml.UriList InstituteAccessServers
        {
            get { return this["InstituteAccessServers"] as Xml.UriList; }
            set { this["InstituteAccessServers"] = value; }
        }

        /// <summary>
        /// Users' home organization for secure internet use
        /// </summary>
        [UserScopedSetting()]
        [Obsolete]
        [NoSettingsVersionUpgrade]
        public string SecureInternetOrganization
        {
            get { return this["SecureInternetOrganization"] as string; }
            set { this["SecureInternetOrganization"] = value; }
        }

        /// <summary>
        /// Last connecting server for secure internet use
        /// </summary>
        [UserScopedSetting()]
        [Obsolete]
        [NoSettingsVersionUpgrade]
        public Uri SecureInternetConnectingServer
        {
            get { return this["SecureInternetConnectingServer"] as Uri; }
            set { this["SecureInternetConnectingServer"] = value; }
        }

        /// <summary>
        /// List of own servers user connects to
        /// </summary>
        [UserScopedSetting()]
        [Obsolete]
        [NoSettingsVersionUpgrade]
        public Xml.UriList OwnServers
        {
            get { return this["OwnServers"] as Xml.UriList; }
            set { this["OwnServers"] = value; }
        }

        /// <summary>
        /// Access token cache
        /// </summary>
        [UserScopedSetting()]
        [Obsolete]
        [NoSettingsVersionUpgrade]
        public Xml.AccessTokenDictionary AccessTokenCache
        {
            get
            {
                return (Xml.AccessTokenDictionary)this["AccessTokenCache"];
            }
            set { this["AccessTokenCache"] = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize settings
        /// </summary>
        public static void Initialize()
        {
            // Changes to AccessTokenCache2 do not propagate to Settings class failing to notice settings need to be persisted.
            Default.AccessTokenCache2.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(Default.AccessTokenCache2.Values))
                    Default.OnPropertyChanged(sender, new PropertyChangedEventArgs(nameof(Default.AccessTokenCache2)));
            };

            if ((Default.SettingsVersion & 0x1) == 0)
            {
                // Migrate settings from previous version.
                Default.Upgrade();
                Default.SettingsVersion |= 0x1;

#pragma warning disable 0612 // This section contains legacy settings conversion.

                // Merge known servers to institute access.
                if (Default.GetPreviousVersion(nameof(InstituteAccessInstanceSourceSettings)) is Xml.InstanceSourceSettings instituteAccessServerSourceSettings &&
                    instituteAccessServerSourceSettings.InstanceSource is Xml.LocalInstanceSourceSettings instituteAccessSourceSettings)
                {
                    foreach (var srv in instituteAccessSourceSettings.ConnectingInstanceList)
                    {
                        try
                        {
                            if (!Default.InstituteAccessServers.Contains(srv.Base))
                                Default.InstituteAccessServers.Add(srv.Base);
                            if (!Default.OwnServers.Contains(srv.Base))
                                Default.OwnServers.Add(srv.Base);
                        }
                        catch { }
                    }
                }

                // Migrate auto-reconnect settings.
                if (Default.GetPreviousVersion(nameof(AutoStartProfile)) is Xml.StartSessionParams autoStartProfile &&
                    autoStartProfile.ConnectingServer is Uri connectingServerBase)
                    Default.LastSelectedServer = connectingServerBase.AbsoluteUri;

                // Migrate OpenVPNForceTCP setting.
                if (Default.GetPreviousVersion(nameof(OpenVPNPreferTCP)) is bool openVPNpreferTCP)
                    Default.PreferTCP = openVPNpreferTCP;
                else if (Default.GetPreviousVersion(nameof(OpenVPNForceTCP)) is bool openVPNForceTCP)
                    Default.PreferTCP = openVPNForceTCP;

                // Migrate OAuth tokens
                if (Default.GetPreviousVersion(nameof(AccessTokenCache)) is Xml.AccessTokenDictionary accessTokenCache)
                    foreach (var token in accessTokenCache)
                        try
                        {
                            ConnectWizard.Engine_SetToken(null, new Engine.SetTokenEventArgs(
                                token.Key,
                                ServerType.Unknown,
                                token.Value.ToJSON()));
                            token.Value.Dispose();
                        }
                        catch { }

#pragma warning restore 0612
            }
        }

        #endregion
    }
}

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Configuration;

namespace eduVPN.Properties
{
    /// <summary>
    /// eduVPN settings
    /// </summary>
    public sealed partial class Settings : ApplicationSettingsBase
    {
        #region Properties

        /// GUID of the installer EXE bundle
        /// </summary>
        [ApplicationScopedSetting()]
        [SettingsDescription("GUID of the installer EXE bundle")]
        public string SelfUpdateBundleId { get; set; }

        /// <summary>
        /// Client identifier
        /// </summary>
        [ApplicationScopedSetting()]
        [SettingsDescription("Client identifier")]
        public string ClientId { get; set; }

        /// <summary>
        /// Client Title
        /// </summary>
        [ApplicationScopedSetting()]
        [SettingsDescription("Client Title")]
        public string ClientTitle { get; set; }

        /// <summary>
        /// Client URI
        /// </summary>
        [ApplicationScopedSetting()]
        [SettingsDescription("Client URI")]
        public Uri ClientAboutUri { get; set; }

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
        /// Perform institute access and own server list cleanup
        /// </summary>
        [UserScopedSetting()]
        [SettingsDescription("Perform institute access and own server list cleanup")]
        public bool CleanupInstituteAccessAndOwnServers { get; set; }

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

#pragma warning disable 0612 // This section contains legacy settings conversion.

                // Merge known servers to institute access.
                if (Default.GetPreviousVersion(nameof(InstituteAccessInstanceSourceSettings)) is Xml.InstanceSourceSettings instituteAccessServerSourceSettings &&
                    instituteAccessServerSourceSettings.InstanceSource is Xml.LocalInstanceSourceSettings instituteAccessSourceSettings)
                {
                    Default.CleanupInstituteAccessAndOwnServers = true;
                    foreach (var srv in instituteAccessSourceSettings.ConnectingInstanceList)
                    {
                        if (!Default.InstituteAccessServers.Contains(srv.Base))
                            Default.InstituteAccessServers.Add(srv.Base);
                        if (!Default.OwnServers.Contains(srv.Base))
                            Default.OwnServers.Add(srv.Base);
                    }
                }

#pragma warning restore 0612
            }
        }

        #endregion
    }
}

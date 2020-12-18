/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using eduVPN.Models;
using System;
using System.Configuration;
using System.Linq;

namespace eduVPN.Properties
{
    /// <summary>
    /// eduVPN settings
    /// </summary>
    public sealed partial class Settings : ApplicationSettingsBase
    {
        #region Fields

        /// <summary>
        /// Instance directory URI IDs as used in <see cref="Properties.Settings.Default"/> collection
        /// </summary>
        public static readonly string[] InstanceDirectoryId = new string[]
        {
            null,
            "SecureInternet",
            "InstituteAccess",
        };

        #endregion

        #region Properties

        /// <summary>
        /// TAP interface name
        /// </summary>
        [UserScopedSetting()]
        [DefaultSettingValue("")]
        [Obsolete("Please use OpenVPNInterfaceID instead")]
        [NoSettingsVersionUpgrade]
        public string OpenVPNInterface
        {
            get { throw new NotSupportedException("OpenVPNInterface is obsolete"); }
            set { throw new NotSupportedException("OpenVPNInterface is obsolete"); }
        }

        /// <summary>
        /// Secure internet configuration history
        /// </summary>
        [UserScopedSetting()]
        [DefaultSettingValue("")]
        [Obsolete("Please use SecureInternetInstanceSourceInfo instead")]
        [NoSettingsVersionUpgrade]
        public Xml.VPNConfigurationSettingsList SecureInternetConfigHistory
        {
            get { throw new NotSupportedException("SecureInternetConfigHistory is obsolete"); }
            set { throw new NotSupportedException("SecureInternetConfigHistory is obsolete"); }
        }

        /// <summary>
        /// Institute access configuration history
        /// </summary>
        [UserScopedSetting()]
        [DefaultSettingValue("")]
        [Obsolete("Please use InstituteAccessInstanceSourceInfo instead")]
        [NoSettingsVersionUpgrade]
        public Xml.VPNConfigurationSettingsList InstituteAccessConfigHistory
        {
            get { throw new NotSupportedException("InstituteAccessConfigHistory is obsolete"); }
            set { throw new NotSupportedException("InstituteAccessConfigHistory is obsolete"); }
        }

        /// <summary>
        /// Access token cache
        /// </summary>
        [UserScopedSetting()]
        [DefaultSettingValue("<SerializableStringDictionary />")]
        [Obsolete("Please use AccessTokenCache instead")]
        [NoSettingsVersionUpgrade]
        public Xml.SerializableStringDictionary AccessTokens
        {
            get { throw new NotSupportedException("AccessTokens is obsolete"); }
            set { throw new NotSupportedException("AccessTokens is obsolete"); }
        }

        /// <summary>
        /// Returns URI-Public key pair from settings
        /// </summary>
        /// <param name="key">The base name of the setting</param>
        /// <returns>URI-Public key pair</returns>
        /// <remarks>When <paramref name="key"/> + "Descr" is not defined, the value is obtained from <paramref name="key"/> and <paramref name="key"/> + "PubKey", which also provide the default fallback values.</remarks>
        public Xml.ResourceRef GetResourceRef(string key)
        {
            return this[key + "Descr"] is Xml.ResourceRef res ?
                res :
                new Xml.ResourceRef()
                {
                    Uri = new Uri((string)this[key]),
                    PublicKey = this[key + "PubKey"] is string pub_key && !String.IsNullOrWhiteSpace(pub_key) ? Convert.FromBase64String(pub_key) : null
                };
        }

        /// <summary>
        /// Secure Internet discovery URL and Ed25519 public key
        /// </summary>
        /// <remarks>When not defined, the value is obtained from <see cref="SecureInternetDiscovery"/> and <see cref="SecureInternetDiscoveryPubKey"/>, which also provide the default fallback values.</remarks>
        [ApplicationScopedSetting()]
        public Xml.ResourceRef SecureInternetDiscoveryDescr
        {
            get { return GetResourceRef("SecureInternetDiscovery"); }
        }

        /// <summary>
        /// Secure Internet discovery URL
        /// </summary>
        [ApplicationScopedSetting()]
        [SpecialSetting(SpecialSetting.WebServiceUrl)]
        [DefaultSettingValue("https://static.eduvpn.nl/disco/secure_internet.json")]
        [Obsolete("Please use SecureInternetDiscoveryDescr instead")]
        public string SecureInternetDiscovery
        {
            get { return ((string)(this["SecureInternetDiscovery"])); }
        }

        /// <summary>
        /// Secure Internet discovery Ed25519 public key
        /// </summary>
        [ApplicationScopedSetting()]
        [DefaultSettingValue("E5On0JTtyUVZmcWd+I/FXRm32nSq8R2ioyW7dcu/U88=")]
        [Obsolete("Please use SecureInternetDiscoveryDescr instead")]
        public string SecureInternetDiscoveryPubKey
        {
            get { return ((string)(this["SecureInternetDiscoveryPubKey"])); }
        }

        /// <summary>
        /// Institute Access discovery URL and Ed25519 public key
        /// </summary>
        /// <remarks>When not defined, the value is obtained from <see cref="InstituteAccessDiscovery"/> and <see cref="InstituteAccessDiscoveryPubKey"/>, which also provide the default fallback values.</remarks>
        [ApplicationScopedSetting()]
        public Xml.ResourceRef InstituteAccessDiscoveryDescr
        {
            get { return GetResourceRef("InstituteAccessDiscovery"); }
        }

        /// <summary>
        /// Institute Access discovery URL
        /// </summary>
        [ApplicationScopedSetting()]
        [SpecialSetting(SpecialSetting.WebServiceUrl)]
        [DefaultSettingValue("https://static.eduvpn.nl/disco/institute_access.json")]
        [Obsolete("Please use InstituteAccessDiscoveryDescr instead")]
        public string InstituteAccessDiscovery
        {
            get { return ((string)(this["InstituteAccessDiscovery"])); }
        }

        /// <summary>
        /// Institute Access discovery Ed25519 public key
        /// </summary>
        [ApplicationScopedSetting()]
        [DefaultSettingValue("E5On0JTtyUVZmcWd+I/FXRm32nSq8R2ioyW7dcu/U88=")]
        [Obsolete("Please use InstituteAccessDiscoveryDescr instead")]
        public string InstituteAccessDiscoveryPubKey
        {
            get { return ((string)(this["InstituteAccessDiscoveryPubKey"])); }
        }

        /// <summary>
        /// Self-updating feature base URI and Ed25519 public key
        /// </summary>
        /// <remarks>When not defined, the value is obtained from <see cref="SelfUpdate"/> and <see cref="SelfUpdatePubKey"/>, which also provide the default fallback values.</remarks>
        [ApplicationScopedSetting()]
        public Xml.ResourceRef SelfUpdateDescr
        {
            get { return GetResourceRef("SelfUpdate"); }
        }

        /// <summary>
        /// Self-updating discovery URL
        /// </summary>
        [ApplicationScopedSetting()]
        [SpecialSetting(SpecialSetting.WebServiceUrl)]
        [DefaultSettingValue("https://static.eduvpn.nl/auto-update/windows.json")]
        [Obsolete("Please use SelfUpdateDescr instead")]
        public string SelfUpdate
        {
            get { return ((string)(this["SelfUpdate"])); }
        }

        /// <summary>
        /// Self-updating discovery Ed25519 public key
        /// </summary>
        [ApplicationScopedSetting()]
        [DefaultSettingValue("15nh06ilJd5f9hbH5rWGgU+qw9IxBHE+j2wVKshidkA=")]
        [Obsolete("Please use SelfUpdateDescr instead")]
        public string SelfUpdatePubKey
        {
            get { return ((string)(this["SelfUpdatePubKey"])); }
        }

        /// <summary>
        /// GUID of the installer EXE bundle
        /// </summary>
        [ApplicationScopedSetting()]
        [SettingsDescription("GUID of the installer EXE bundle")]
        public string SelfUpdateBundleId { get; set; }

        /// <summary>
        /// Client ID
        /// </summary>
        [ApplicationScopedSetting()]
        [SettingsDescription("Client ID")]
        public string ClientId { get; set; }

        /// <summary>
        /// Client Title
        /// </summary>
        [ApplicationScopedSetting()]
        [SettingsDescription("Client Title")]
        public string ClientTitle { get; set; }

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

                // Versions before 1.0.4 used interface name, instead of ID.
                if (Default.GetPreviousVersion("OpenVPNInterface") is string iface_name &&
                    NetworkInterface.TryFromName(iface_name, out var iface))
                    Default.OpenVPNInterfaceID = iface.Id;

                for (int source_index = (int)InstanceSourceType._start; source_index < (int)InstanceSourceType._end; source_index++)
                {
                    #pragma warning disable 0612 // This section contains legacy settings conversion.
                    if (Default.GetPreviousVersion(InstanceDirectoryId[source_index] + "ConfigHistory") is Xml.VPNConfigurationSettingsList settings_list &&
                        settings_list.Count > 0)
                    {
                        // Versions before 1.0.9 used different instance source settings. Convert them.
                        Xml.InstanceSourceSettingsBase h = null;
                        if (settings_list[0] is Xml.LocalVPNConfigurationSettings)
                        {
                            // Local authenticating instance source:
                            // - Convert instance list.
                            // - Set connecting instance by maximum popularity.
                            var h_local = new Xml.LocalInstanceSourceSettings();
                            foreach (var h_cfg in settings_list)
                            {
                                if (h_cfg is Xml.LocalVPNConfigurationSettings h_cfg_local)
                                {
                                    var instance = h_local.ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_cfg_local.Instance.AbsoluteUri);
                                    if (instance == null)
                                    {
                                        // Add (instance, profile) pair.
                                        h_local.ConnectingInstanceList.Add(new Xml.InstanceRef()
                                        {
                                            Base = h_cfg_local.Instance,
                                            Popularity = h_cfg_local.Popularity,
                                            ProfileList = new Xml.ProfileRefList()
                                            {
                                                new Xml.ProfileRef()
                                                {
                                                    Id = h_cfg_local.Profile,
                                                    Popularity = h_cfg_local.Popularity
                                                }
                                            }
                                        });
                                    }
                                    else
                                    {
                                        // Instance already on the list. Update it.
                                        instance.Popularity = Math.Max(instance.Popularity, h_cfg_local.Popularity);
                                        if (instance.ProfileList.FirstOrDefault(prof => prof.Id == h_cfg_local.Profile) == null)
                                        {
                                            // Add profile to the instance.
                                            instance.ProfileList.Add(new Xml.ProfileRef()
                                            {
                                                Id = h_cfg_local.Profile,
                                                Popularity = h_cfg_local.Popularity
                                            });
                                        }
                                    }
                                }
                            }
                            h_local.ConnectingInstance = h_local.ConnectingInstanceList.Count > 0 ? h_local.ConnectingInstanceList.Aggregate((most_popular_instance, inst) => (most_popular_instance == null || inst.Popularity > most_popular_instance.Popularity ? inst : most_popular_instance))?.Base : null;
                            h = h_local;
                        }
                        else if (settings_list[0] is Xml.DistributedVPNConfigurationSettings h_cfg_distributed)
                        {
                            // Distributed authenticating instance source:
                            // - Convert authenticating instance.
                            // - Convert connecting instance.
                            h = new Xml.DistributedInstanceSourceSettings
                            {
                                AuthenticatingInstance = new Uri(h_cfg_distributed.AuthenticatingInstance),
                                ConnectingInstance = new Uri(h_cfg_distributed.LastInstance)
                            };
                        }
                        else if (settings_list[0] is Xml.FederatedVPNConfigurationSettings h_cfg_federated)
                        {
                            // Federated authenticating instance source:
                            // - Convert connecting instance.
                            h = new Xml.FederatedInstanceSourceSettings
                            {
                                ConnectingInstance = new Uri(h_cfg_federated.LastInstance)
                            };
                        }

                        Default[InstanceDirectoryId[source_index] + "InstanceSourceSettings"] = new Xml.InstanceSourceSettings() { InstanceSource = h };
                    }
                    #pragma warning restore 0612
                }

                // Versions before 1.0.14 used string dictionary for access token cache.
                if (Default.GetPreviousVersion("AccessTokens") is Xml.SerializableStringDictionary access_tokens)
                    foreach (var token in access_tokens)
                    {
                        var authorization_endpoint = new Uri(token.Key);

                        // Carefully decode access token as it might be damaged or encrypted using another session key.
                        try { Default.AccessTokenCache[authorization_endpoint.GetLeftPart(UriPartial.Authority) + "/"] = AccessToken.FromBase64String(token.Value); }
                        catch { };
                    }
            }
        }

        #endregion
     }
}

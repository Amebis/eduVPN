/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Configuration;

namespace eduVPN.Properties
{
    public sealed partial class Settings : ApplicationSettingsBase
    {
        [UserScopedSetting()]
        [DefaultSettingValue("")]
        [Obsolete("Please use OpenVPNInterfaceID instead")]
        [NoSettingsVersionUpgrade]
        public string OpenVPNInterface
        {
            get { throw new NotSupportedException("OpenVPNInterface is obsolete"); }
            set { throw new NotSupportedException("OpenVPNInterface is obsolete"); }
        }

        [UserScopedSetting()]
        [DefaultSettingValue("")]
        [Obsolete("Please use SecureInternetInstanceSourceInfo instead")]
        [NoSettingsVersionUpgrade]
        public Xml.VPNConfigurationSettingsList SecureInternetConfigHistory
        {
            get { throw new NotSupportedException("SecureInternetConfigHistory is obsolete"); }
            set { throw new NotSupportedException("SecureInternetConfigHistory is obsolete"); }
        }

        [UserScopedSetting()]
        [DefaultSettingValue("")]
        [Obsolete("Please use InstituteAccessInstanceSourceInfo instead")]
        [NoSettingsVersionUpgrade]
        public Xml.VPNConfigurationSettingsList InstituteAccessConfigHistory
        {
            get { throw new NotSupportedException("InstituteAccessConfigHistory is obsolete"); }
            set { throw new NotSupportedException("InstituteAccessConfigHistory is obsolete"); }
        }

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
                    PublicKey = this[key + "PubKey"] is string pub_key && !string.IsNullOrWhiteSpace(pub_key) ? Convert.FromBase64String(pub_key) : null
                };
        }

        /// <summary>
        /// Secure Internet discovery URL and Ed25519 public key
        /// </summary>
        /// <remarks>When not defined, the value is obtained from <c>SecureInternetDiscovery</c> and <c>SecureInternetDiscoveryPubKey</c>, which also provide the default fallback values.</remarks>
        [ApplicationScopedSetting()]
        public Xml.ResourceRef SecureInternetDiscoveryDescr
        {
            get { return GetResourceRef("SecureInternetDiscovery"); }
        }

        [ApplicationScopedSetting()]
        [SpecialSetting(SpecialSetting.WebServiceUrl)]
        [DefaultSettingValue("https://static.eduvpn.nl/disco/secure_internet.json")]
        [Obsolete("Please use SecureInternetDiscoveryDescr instead")]
        public string SecureInternetDiscovery
        {
            get { return ((string)(this["SecureInternetDiscovery"])); }
        }

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
        /// <remarks>When not defined, the value is obtained from <c>InstituteAccessDiscovery</c> and <c>InstituteAccessDiscoveryPubKey</c>, which also provide the default fallback values.</remarks>
        [ApplicationScopedSetting()]
        public Xml.ResourceRef InstituteAccessDiscoveryDescr
        {
            get { return GetResourceRef("InstituteAccessDiscovery"); }
        }

        [ApplicationScopedSetting()]
        [SpecialSetting(SpecialSetting.WebServiceUrl)]
        [DefaultSettingValue("https://static.eduvpn.nl/disco/institute_access.json")]
        [Obsolete("Please use InstituteAccessDiscoveryDescr instead")]
        public string InstituteAccessDiscovery
        {
            get { return ((string)(this["InstituteAccessDiscovery"])); }
        }

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
        /// <remarks>When not defined, the value is obtained from <c>SelfUpdate</c> and <c>SelfUpdatePubKey</c>, which also provide the default fallback values.</remarks>
        [ApplicationScopedSetting()]
        public Xml.ResourceRef SelfUpdateDescr
        {
            get { return GetResourceRef("SelfUpdate"); }
        }

        [ApplicationScopedSetting()]
        [SpecialSetting(SpecialSetting.WebServiceUrl)]
        [DefaultSettingValue("https://static.eduvpn.nl/auto-update/windows.json")]
        [Obsolete("Please use SelfUpdateDescr instead")]
        public string SelfUpdate
        {
            get { return ((string)(this["SelfUpdate"])); }
        }

        [ApplicationScopedSetting()]
        [DefaultSettingValue("15nh06ilJd5f9hbH5rWGgU+qw9IxBHE+j2wVKshidkA=")]
        [Obsolete("Please use SelfUpdateDescr instead")]
        public string SelfUpdatePubKey
        {
            get { return ((string)(this["SelfUpdatePubKey"])); }
        }
    }
}

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Xml;
using Microsoft.Win32;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;

namespace eduVPN.Properties
{
    /// <summary>
    /// Settings wrapper to support configuration overriding using registry
    /// </summary>
    public class SettingsEx : ApplicationSettingsBase
    {
        #region Fields

        /// <summary>
        /// Application settings override registry key
        /// </summary>
        private RegistryKey Key;

        #endregion

        #region Properties

        /// <summary>
        /// Default settings
        /// </summary>
        public static SettingsEx Default { get; } = ((SettingsEx)Synchronized(new SettingsEx()));

        /// <summary>
        /// Registry key path
        /// </summary>
        public string RegistryKeyPath
        {
            get { return _RegistryKeyPath; }
            set
            {
                _RegistryKeyPath = value;
                Key?.Dispose();
                Key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default).OpenSubKey(value, false);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _RegistryKeyPath;

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

        #endregion

        #region Methods

        private bool GetValue<T>(string name, out T value) where T : IRegistrySerializable, new()
        {
            if (Key != null)
            {
                var v = new T();
                if (v.ReadRegistry(Key, name))
                {
                    value = v;
                    return true;
                }
            }
            value = default;
            return false;
        }

        private bool GetValue(string name, out string value)
        {
            if (Key?.GetValue(name) is string v)
            {
                value = v;
                return true;
            }
            value = default;
            return false;
        }

        private bool GetValue(string name, out string[] value)
        {
            if (Key?.GetValue(name) is string[] v)
            {
                value = v;
                return true;
            }
            value = default;
            return false;
        }

        private bool GetValue(string name, out StringCollection value)
        {
            if (Key?.GetValue(name) is string[] v)
            {
                value = new StringCollection();
                value.AddRange(v);
                return true;
            }
            value = default;
            return false;
        }

        #endregion
    }
}

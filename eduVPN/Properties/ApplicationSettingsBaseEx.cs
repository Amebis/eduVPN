/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Xml;
using Microsoft.Win32;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Linq;

namespace eduVPN.Properties
{
    /// <summary>
    /// Settings wrapper to support configuration overriding using registry
    /// </summary>
    public class ApplicationSettingsBaseEx : ApplicationSettingsBase
    {
        #region Fields

        /// <summary>
        /// Application settings override registry key
        /// </summary>
        private RegistryKey Key;

        #endregion

        #region Properties

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

        #endregion

        #region Methods

        protected bool GetValue<T>(string name, out T value) where T : IRegistrySerializable, new()
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

        protected bool GetValue(string name, out uint value)
        {
            if (Key?.GetValue(name) is int v)
            {
                value = (uint)v;
                return true;
            }
            value = default;
            return false;
        }

        protected bool GetValue(string name, out bool value)
        {
            if (Key?.GetValue(name) is int v)
            {
                value = v != 0;
                return true;
            }
            value = default;
            return false;
        }

        protected bool GetValue(string name, out string value)
        {
            if (Key?.GetValue(name) is string v)
            {
                value = v;
                return true;
            }
            value = default;
            return false;
        }

        protected bool GetValue(string name, out string[] value)
        {
            if (Key?.GetValue(name) is string[] v)
            {
                value = v;
                return true;
            }
            value = default;
            return false;
        }

        protected bool GetValue(string name, out StringCollection value)
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

        protected bool GetValue(string name, out UriList value)
        {
            if (Key?.GetValue(name) is string[] v)
            {
                value = new UriList();
                value.AddRange(v.Select(entry => new Uri(entry)));
                return true;
            }
            value = default;
            return false;
        }

        #endregion
    }
}

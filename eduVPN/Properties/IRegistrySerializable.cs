/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.Win32;

namespace eduVPN.Properties
{
    /// <summary>
    /// Provides registry serialization and deserialization
    /// </summary>
    public interface IRegistrySerializable
    {
        bool ReadRegistry(RegistryKey key, string name);
    }
}

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduVPN.Models
{
    /// <summary>
    /// Network interface
    /// </summary>
    public class NetworkInterface
    {
        #region Properties

        /// <summary>
        /// Interface identifier
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Interface name
        /// </summary>
        public string Name { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a network interface
        /// </summary>
        /// <param name="id">Interface identifier</param>
        /// <param name="name">Interface name</param>
        public NetworkInterface(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Returns network interface by identifier
        /// </summary>
        /// <param name="id">Interface identifier</param>
        /// <param name="iface">Network interface</param>
        /// <returns><c>true</c> if interface found; <c>false</c> otherwise</returns>
        public static bool TryFromId(Guid id, out NetworkInterface iface)
        {
            foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (Guid.TryParse(nic.Id, out var nicId) && id == nicId)
                {
                    iface = new NetworkInterface(id, nic.Name);
                    return true;
                }
            }

            iface = null;
            return false;
        }

        /// <summary>
        /// Returns network interface by identifier
        /// </summary>
        /// <param name="id">Interface identifier</param>
        /// <returns>Network interface</returns>
        /// <exception cref="ArgumentOutOfRangeException">Network interface with given identifier not found.</exception>
        public static NetworkInterface FromId(Guid id)
        {
            if (TryFromId(id, out var iface))
                return iface;

            throw new ArgumentOutOfRangeException(nameof(id), string.Format(Resources.Strings.ErrorNetworkInterfaceIdNotFound, id));
        }

        /// <summary>
        /// Returns network interface by name
        /// </summary>
        /// <param name="name">Interface name</param>
        /// <param name="iface">Network interface</param>
        /// <returns><c>true</c> if interface found; <c>false</c> otherwise</returns>
        public static bool TryFromName(string name, out NetworkInterface iface)
        {
            var nameLC = name.ToLower();
            foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nameLC == nic.Name.ToLower())
                {
                    iface = new NetworkInterface(Guid.TryParse(nic.Id, out var nicId) ? nicId : default, nic.Name);
                    return true;
                }
            }

            iface = null;
            return false;
        }

        /// <summary>
        /// Returns network interface by name
        /// </summary>
        /// <param name="name">Interface name</param>
        /// <returns>Network interface</returns>
        /// <exception cref="ArgumentOutOfRangeException">Network interface with given name not found.</exception>
        public static NetworkInterface FromName(string name)
        {
            if (TryFromName(name, out var iface))
                return iface;

            throw new ArgumentOutOfRangeException(nameof(name), string.Format(Resources.Strings.ErrorNetworkInterfaceNameNotFound, name));
        }

        #endregion
    }
}

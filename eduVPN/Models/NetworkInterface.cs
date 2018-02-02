/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;

namespace eduVPN.Models
{
    /// <summary>
    /// Network interface
    /// </summary>
    public class NetworkInterface : BindableBase
    {
        #region Properties

        /// <summary>
        /// Interface ID
        /// </summary>
        public Guid ID { get; }

        /// <summary>
        /// Interface name
        /// </summary>
        public string Name { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a network interface
        /// </summary>
        /// <param name="id">Interface ID</param>
        /// <param name="name">Interface name</param>
        public NetworkInterface(Guid id, string name)
        {
            ID = id;
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
        /// Returns network interface by ID
        /// </summary>
        /// <param name="id">Interface ID</param>
        /// <param name="iface">Network interface</param>
        /// <returns><c>true</c> if interface found; <c>false</c> otherwise</returns>
        public static bool TryFromID(Guid id, out NetworkInterface iface)
        {
            foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (Guid.TryParse(nic.Id, out var nic_id) && id == nic_id)
                {
                    iface = new NetworkInterface(id, nic.Name);
                    return true;
                }
            }

            iface = null;
            return false;
        }

        /// <summary>
        /// Returns network interface by ID
        /// </summary>
        /// <param name="id">Interface ID</param>
        /// <returns>Network interface</returns>
        /// <exception cref="ArgumentOutOfRangeException">Network interface with given ID not found.</exception>
        public static NetworkInterface FromID(Guid id)
        {
            if (TryFromID(id, out var iface))
                return iface;

            throw new ArgumentOutOfRangeException("id", String.Format(Resources.Strings.ErrorNetworkInterfaceIDNotFound, id));
        }

        /// <summary>
        /// Returns network interface by name
        /// </summary>
        /// <param name="name">Interface name</param>
        /// <param name="iface">Network interface</param>
        /// <returns><c>true</c> if interface found; <c>false</c> otherwise</returns>
        public static bool TryFromName(string name, out NetworkInterface iface)
        {
            var name_lc = name.ToLower();
            foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (name_lc == nic.Name.ToLower())
                {
                    iface = new NetworkInterface(Guid.TryParse(nic.Id, out var nic_id) ? nic_id : default(Guid), nic.Name);
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

            throw new ArgumentOutOfRangeException("name", String.Format(Resources.Strings.ErrorNetworkInterfaceNameNotFound, name));
        }

        #endregion
    }
}

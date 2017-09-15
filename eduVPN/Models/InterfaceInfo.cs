/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Net.NetworkInformation;

namespace eduVPN.Models
{
    /// <summary>
    /// Network interface
    /// </summary>
    public class InterfaceInfo : BindableBase
    {
        #region Properties

        /// <summary>
        /// Interface ID
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
        /// <param name="id">Interface ID</param>
        /// <param name="name">Interface name</param>
        public InterfaceInfo(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        #endregion

        #region Methods

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
        public static bool TryFromId(Guid id, out InterfaceInfo iface)
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (Guid.TryParse(nic.Id, out var nic_id) && id == nic_id)
                {
                    iface = new InterfaceInfo(id, nic.Name);
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
        public static InterfaceInfo FromId(Guid id)
        {
            if (TryFromId(id, out var iface))
                return iface;

            throw new ArgumentOutOfRangeException("id", String.Format(Resources.Strings.ErrorNetworkInterfaceIDNotFound, id));
        }

        /// <summary>
        /// Returns network interface by name
        /// </summary>
        /// <param name="name">Interface name</param>
        /// <param name="iface">Network interface</param>
        /// <returns><c>true</c> if interface found; <c>false</c> otherwise</returns>
        public static bool TryFromName(string name, out InterfaceInfo iface)
        {
            var name_lc = name.ToLower();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (name_lc == nic.Name.ToLower())
                {
                    iface = new InterfaceInfo(Guid.TryParse(nic.Id, out var nic_id) ? nic_id : default(Guid), nic.Name);
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
        public static InterfaceInfo FromName(string name)
        {
            if (TryFromName(name, out var iface))
                return iface;

            throw new ArgumentOutOfRangeException("name", String.Format(Resources.Strings.ErrorNetworkInterfaceNameNotFound, name));
        }

        #endregion
    }
}

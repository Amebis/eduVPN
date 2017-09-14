/*
    eduVPN - End-user friendly VPN

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
        public string DisplayName { get; }

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
            DisplayName = name;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return DisplayName;
        }

        #endregion
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;

namespace eduVPN.Models
{
    /// <summary>
    /// 2-Factor Authentication method info
    /// </summary>
    public class TwoFactorAuthenticationMethodInfo : BindableBase
    {
        #region Properties

        /// <summary>
        /// Method ID to be used as username
        /// </summary>
        public string ID { get; }

        /// <summary>
        /// Method name
        /// </summary>
        public string DisplayName { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a method info
        /// </summary>
        public TwoFactorAuthenticationMethodInfo()
        {
        }

        /// <summary>
        /// Constructs a method info
        /// </summary>
        /// <param name="id">Method ID to be used as username</param>
        /// <param name="name">Method name</param>
        public TwoFactorAuthenticationMethodInfo(string id, string name)
        {
            ID = id;
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

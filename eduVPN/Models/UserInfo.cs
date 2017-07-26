/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// eduVPN user info
    /// </summary>
    public class UserInfo : BindableBase, JSON.ILoadableItem
    {
        #region Properties

        /// <summary>
        /// Is user enabled?
        /// </summary>
        public bool IsEnabled
        {
            get { return _is_enabled; }
            set { if (value != _is_enabled) { _is_enabled = value; RaisePropertyChanged(); } }
        }
        private bool _is_enabled;

        /// <summary>
        /// Is two-factor authentication enrolled for this user?
        /// </summary>
        public bool IsTwoFactorAuthentication
        {
            get { return _is_two_factor_authentication; }
            set { if (value != _is_two_factor_authentication) { _is_two_factor_authentication = value; RaisePropertyChanged(); } }
        }
        private bool _is_two_factor_authentication;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs user info
        /// </summary>
        public UserInfo()
        {
            IsEnabled = true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads user info from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary. <c>is_disabled</c> and <c>two_factor_enrolled</c> elements while both optional, they should be booleans.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public void Load(object obj)
        {
            var obj2 = obj as Dictionary<string, object>;
            if (obj2 == null)
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(Dictionary<string, object>), obj.GetType());

            // Set two-factor authentication.
            IsEnabled = eduJSON.Parser.GetValue(obj2, "is_disabled", out bool is_disabled) ? !is_disabled : true;

            // Set two-factor authentication.
            IsTwoFactorAuthentication = eduJSON.Parser.GetValue(obj2, "two_factor_enrolled", out bool two_factor_enrolled) ? two_factor_enrolled : false;
        }

        #endregion
    }
}

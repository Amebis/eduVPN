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

        /// <summary>
        /// Enrolled 2-Factor authentication methods
        /// </summary>
        public TwoFactorAuthenticationMethods TwoFactorMethods
        {
            get { return _two_factor_methods; }
            set { if (value != _two_factor_methods) { _two_factor_methods = value; RaisePropertyChanged(); } }
        }
        private TwoFactorAuthenticationMethods _two_factor_methods;

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

        #region ILoadableItem Support

        /// <summary>
        /// Loads user info from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary. <c>is_disabled</c> and <c>two_factor_enrolled</c> elements while both optional, they should be booleans.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public void Load(object obj)
        {
            if (obj is Dictionary<string, object> obj2)
            {
                // Set two-factor authentication.
                IsEnabled = eduJSON.Parser.GetValue(obj2, "is_disabled", out bool is_disabled) ? !is_disabled : true;

                // Set two-factor authentication.
                IsTwoFactorAuthentication = eduJSON.Parser.GetValue(obj2, "two_factor_enrolled", out bool two_factor_enrolled) ? two_factor_enrolled : false;
                if (IsTwoFactorAuthentication && eduJSON.Parser.GetValue(obj2, "two_factor_enrolled_with", out List<object> two_factor_enrolled_with))
                {
                    TwoFactorMethods = TwoFactorAuthenticationMethods.None;
                    foreach (var method in two_factor_enrolled_with)
                        if (method is string method_str)
                            switch (method_str)
                            {
                                case "totp": TwoFactorMethods |= TwoFactorAuthenticationMethods.TOTP; break;
                                case "yubi": TwoFactorMethods |= TwoFactorAuthenticationMethods.YubiKey; break;
                            }
                }
                else
                    TwoFactorMethods = TwoFactorAuthenticationMethods.None;
            }
            else
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(Dictionary<string, object>), obj.GetType());
        }

        #endregion
    }
}

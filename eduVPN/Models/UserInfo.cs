/*
    eduVPN - VPN for education and research

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
        /// User ID
        /// </summary>
        public string ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }
        private string _id;

        /// <summary>
        /// Is user enabled?
        /// </summary>
        public bool IsEnabled
        {
            get { return _is_enabled; }
            set { SetProperty(ref _is_enabled, value); }
        }
        private bool _is_enabled = true;

        /// <summary>
        /// Is two-factor authentication enrolled for this user?
        /// </summary>
        public bool IsTwoFactorAuthentication
        {
            get { return _is_two_factor_authentication; }
            set { SetProperty(ref _is_two_factor_authentication, value); }
        }
        private bool _is_two_factor_authentication;

        /// <summary>
        /// Enrolled 2-Factor authentication methods
        /// </summary>
        public TwoFactorAuthenticationMethods TwoFactorMethods
        {
            get { return _two_factor_methods; }
            set { SetProperty(ref _two_factor_methods, value); }
        }
        private TwoFactorAuthenticationMethods _two_factor_methods;

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return ID;
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
                // Set user ID.
                ID = eduJSON.Parser.GetValue(obj2, "user_id", out string user_id) ? user_id : null;

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
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());
        }

        #endregion
    }
}

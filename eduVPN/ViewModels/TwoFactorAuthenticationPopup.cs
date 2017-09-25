/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// 2-Factor Authentication pop-up
    /// </summary>
    public class TwoFactorAuthenticationPopup : PasswordPopup
    {
        #region Properties

        /// <summary>
        /// Authentication method list
        /// </summary>
        public ObservableCollection<TwoFactorAuthenticationBasePanel> MethodList
        {
            get { return _method_list; }
        }
        private ObservableCollection<TwoFactorAuthenticationBasePanel> _method_list;

        /// <summary>
        /// 2-Factor authentication method
        /// </summary>
        public TwoFactorAuthenticationBasePanel SelectedMethod
        {
            get { return _selected_method; }
            set { if (value != _selected_method) { _selected_method = value; RaisePropertyChanged(); } }
        }
        private TwoFactorAuthenticationBasePanel _selected_method;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a pop-up window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public TwoFactorAuthenticationPopup(object sender, eduOpenVPN.Management.UsernamePasswordAuthenticationRequestedEventArgs e) :
            base(sender, e)
        {
            // Query supported & enrolled 2-Factor authentication methods.
            TwoFactorAuthenticationMethods methods = TwoFactorAuthenticationMethods.Any;
            if (Session.Configuration.ConnectingProfile.IsTwoFactorAuthentication)
                methods &= Session.Configuration.ConnectingProfile.TwoFactorMethods;
            if (Session.UserInfo.IsTwoFactorAuthentication)
                methods &= Session.UserInfo.TwoFactorMethods;

            // Prepare the list of methods.
            _method_list = new ObservableCollection<TwoFactorAuthenticationBasePanel>();
            if (methods.HasFlag(TwoFactorAuthenticationMethods.TOTP))
                _method_list.Add(new TOTPAuthenticationPanel());
            if (methods.HasFlag(TwoFactorAuthenticationMethods.YubiKey))
                _method_list.Add(new YubiKeyAuthenticationPanel());

            // Initially select the first method.
            if (_method_list.Count > 0)
                SelectedMethod = _method_list[0];
        }

        #endregion
    }
}

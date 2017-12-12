/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOpenVPN.Management;
using eduVPN.Models;
using eduVPN.ViewModels.Panels;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace eduVPN.ViewModels.Windows
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
            set
            {
                if (SetProperty(ref _selected_method, value))
                    RaisePropertyChanged(nameof(ApplyResponse));
            }
        }
        private TwoFactorAuthenticationBasePanel _selected_method;

        /// <inheritdoc/>
        public override ICommand ApplyResponse
        {
            get { return SelectedMethod != null ? SelectedMethod.ApplyResponse : base.ApplyResponse; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a pop-up window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public TwoFactorAuthenticationPopup(object sender, UsernamePasswordAuthenticationRequestedEventArgs e) :
            base(sender, e)
        {
            // Query profile supported & user enrolled 2-Factor authentication methods.
            var methods = TwoFactorAuthenticationMethods.Any;
            if (Session.ConnectingProfile.IsTwoFactorAuthentication)
                methods &= Session.ConnectingProfile.TwoFactorMethods;
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
                _selected_method = _method_list[0];
        }

        #endregion
    }
}

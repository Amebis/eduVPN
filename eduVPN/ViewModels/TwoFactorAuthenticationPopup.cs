/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using System.Collections.ObjectModel;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// 2-Factor Authentication pop-up
    /// </summary>
    public class TwoFactorAuthenticationPopup : UsernamePasswordPopup
    {
        #region Properties

        /// <summary>
        /// Authentication method list
        /// </summary>
        public ObservableCollection<Models.TwoFactorAuthenticationMethodInfo> MethodList
        {
            get { return _method_list; }
            set { if (value != _method_list) { _method_list = value; RaisePropertyChanged(); } }
        }
        private ObservableCollection<Models.TwoFactorAuthenticationMethodInfo> _method_list;

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
            MethodList = new ObservableCollection<TwoFactorAuthenticationMethodInfo>();
            if (methods.HasFlag(TwoFactorAuthenticationMethods.TOTP))
                MethodList.Add(new TwoFactorAuthenticationMethodInfo("totp", Resources.Strings.TwoFactorAuthenticationMethodTOTP));
            if (methods.HasFlag(TwoFactorAuthenticationMethods.YubiKey))
                MethodList.Add(new TwoFactorAuthenticationMethodInfo("yubi", Resources.Strings.TwoFactorAuthenticationMethodYubiKey));

            // Initially select the first method.
            if (MethodList.Count > 0)
                Username = MethodList[0].ID;
        }

        #endregion
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Panels;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace eduVPN.ViewModels.Windows
{
    /// <summary>
    /// 2-Factor Authentication enrollment pop-up
    /// </summary>
    public class TwoFactorEnrollmentPopup : Window
    {
        #region Properties

        /// <summary>
        /// User
        /// </summary>
        public UserInfo User { get; }

        /// <summary>
        /// Authenticating instance
        /// </summary>
        public Instance AuthenticatingInstance { get; }

        /// <summary>
        /// Connecting profile
        /// </summary>
        public Profile Profile { get; }

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
                    RaisePropertyChanged(nameof(ApplyEnrollment));
            }
        }
        private TwoFactorAuthenticationBasePanel _selected_method;

        /// <summary>
        /// Apply enrollment command
        /// </summary>
        public ICommand ApplyEnrollment
        {
            get { return SelectedMethod?.ApplyEnrollment; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a pop-up window
        /// </summary>
        /// <param name="sender">Profile select panel</param>
        /// <param name="e">Event parameters</param>
        public TwoFactorEnrollmentPopup(object sender, RequestTwoFactorEnrollmentEventArgs e)
        {
            var selection_panel = sender as ConnectingSelectPanel;

            User = e.User;
            AuthenticatingInstance = e.AuthenticatingInstance;
            Profile = e.Profile;

            // Prepare the list of methods.
            var last_method = Properties.Settings.Default.InstanceSettings.TryGetValue(AuthenticatingInstance.Base.AbsoluteUri, out var settings) ? settings.LastTwoFactorAuthenticationMethod : null;
            _method_list = new ObservableCollection<TwoFactorAuthenticationBasePanel>();
            TwoFactorAuthenticationBasePanel method;
            if (Profile.TwoFactorMethods.HasFlag(TwoFactorAuthenticationMethods.TOTP))
            {
                _method_list.Add(method = new TOTPEnrollmentPanel(selection_panel.Wizard, AuthenticatingInstance));
                if (last_method == method.ID) _selected_method = method;
            }
            if (Profile.TwoFactorMethods.HasFlag(TwoFactorAuthenticationMethods.YubiKey))
            {
                _method_list.Add(method = new YubiKeyAuthenticationPanel(selection_panel.Wizard, AuthenticatingInstance));
                if (last_method == method.ID) _selected_method = method;
            }
        }

        #endregion
    }
}

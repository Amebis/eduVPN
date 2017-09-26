/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOpenVPN.Management;
using Prism.Commands;
using System.Windows.Input;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Password authentication pop-up
    /// </summary>
    public class PasswordPopup : Window
    {
        #region Properties

        /// <summary>
        /// VPN session
        /// </summary>
        public VPNSession Session
        {
            get { return _session; }
            set { if (value != _session) { _session = value; RaisePropertyChanged(); } }
        }
        private VPNSession _session;

        /// <summary>
        /// Authenticating realm
        /// </summary>
        public string Realm
        {
            get { return _realm; }
            set { if (value != _realm) { _realm = value; RaisePropertyChanged(); } }
        }
        private string _realm;

        /// <summary>
        /// Apply response command
        /// </summary>
        public virtual ICommand ApplyResponse
        {
            get
            {
                if (_apply_response == null)
                {
                    _apply_response = new DelegateCommand<PasswordAuthenticationRequestedEventArgs>(
                        // execute
                        e =>
                        {
                            // Password cannot be set using MVVP, since <PasswordBox> control does not support binding.
                        },

                        // canExecute
                        e => e is PasswordAuthenticationRequestedEventArgs);
                }

                return _apply_response;
            }
        }
        private DelegateCommand<PasswordAuthenticationRequestedEventArgs> _apply_response;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a pop-up window
        /// </summary>
        /// <param name="sender">VPN session</param>
        /// <param name="e"></param>
        public PasswordPopup(object sender, PasswordAuthenticationRequestedEventArgs e)
        {
            Session = sender as VPNSession;
            Realm = e.Realm;
        }

        #endregion
    }
}

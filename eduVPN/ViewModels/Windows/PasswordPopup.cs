/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOpenVPN.Management;
using eduVPN.ViewModels.VPN;
using Prism.Commands;
using System.Diagnostics;
using System.Windows.Input;

namespace eduVPN.ViewModels.Windows
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
        public VPNSession Session { get; }

        /// <summary>
        /// Authenticating realm
        /// </summary>
        public string Realm
        {
            get { return _realm; }
            set { SetProperty(ref _realm, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand<PasswordAuthenticationRequestedEventArgs> _apply_response;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a pop-up window
        /// </summary>
        /// <param name="sender">VPN session</param>
        /// <param name="e">Event arguments</param>
        public PasswordPopup(object sender, PasswordAuthenticationRequestedEventArgs e)
        {
            Session = sender as VPNSession;
            _realm = e.Realm;
        }

        #endregion
    }
}

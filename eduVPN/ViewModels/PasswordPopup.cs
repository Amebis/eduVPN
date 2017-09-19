/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

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

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a pop-up window
        /// </summary>
        /// <param name="sender">VPN session</param>
        /// <param name="e"></param>
        public PasswordPopup(object sender, eduOpenVPN.Management.PasswordAuthenticationRequestedEventArgs e)
        {
            Session = sender as ViewModels.VPNSession;
            Realm = e.Realm;
        }

        #endregion
    }
}

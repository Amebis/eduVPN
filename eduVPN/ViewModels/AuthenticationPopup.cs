/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Authentication pop-up
    /// </summary>
    public class AuthenticationPopup : Window
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
        /// User name
        /// </summary>
        public string UserName
        {
            get { return _username; }
            set { if (value != _username) { _username = value; RaisePropertyChanged(); } }
        }
        private string _username;

        #endregion
    }
}

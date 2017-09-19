/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Username and password authentication pop-up
    /// </summary>
    public class UsernamePasswordPopup : PasswordPopup
    {
        #region Properties

        /// <summary>
        /// Username
        /// </summary>
        public string Username
        {
            get { return _username; }
            set { if (value != _username) { _username = value; RaisePropertyChanged(); } }
        }
        private string _username;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a pop-up window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public UsernamePasswordPopup(object sender, eduOpenVPN.Management.UsernamePasswordAuthenticationRequestedEventArgs e) :
            base(sender, e)
        {
        }

        #endregion
    }
}

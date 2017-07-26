/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Secure internet selection wizard page
    /// </summary>
    public class SecureInternetSelectPage : InstanceSelectPage
    {
        #region Constructors

        /// <summary>
        /// Constructs a secure internet selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public SecureInternetSelectPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion
    }
}

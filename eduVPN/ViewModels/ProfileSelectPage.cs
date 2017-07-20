/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Profile selection wizard page
    /// </summary>
    class ProfileSelectPage : ConnectWizardPage
    {
        #region Constructors

        /// <summary>
        /// Constructs an profile selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public ProfileSelectPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion
    }
}

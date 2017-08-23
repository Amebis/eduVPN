/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Initial page to notify user the application is loading
    /// </summary>
    public class InitializingPage : ConnectWizardPage
    {
        #region Constructors

        /// <summary>
        /// Constructs a initial wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public InitializingPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion
    }
}

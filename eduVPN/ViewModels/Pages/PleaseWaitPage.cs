/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Please wait page
    /// </summary>
    public class PleaseWaitPage : ConnectWizardStandardPage
    {
        #region Constructors

        /// <summary>
        /// Constructs a waiting page.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public PleaseWaitPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion
    }
}

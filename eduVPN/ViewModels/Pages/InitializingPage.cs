/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Initial page to notify user the application is loading
    /// </summary>
    public class InitializingPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Initialization progress value
        /// </summary>
        public Range<int> Progress
        {
            get { return _progress; }
            set { SetProperty(ref _progress, value); }
        }
        private Range<int> _progress;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a initial wizard page.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public InitializingPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion
    }
}

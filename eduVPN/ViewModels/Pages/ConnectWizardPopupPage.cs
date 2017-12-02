/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// A page that is displayed temporary outside of regular Wizard flow
    /// </summary>
    public class ConnectWizardPopupPage : ConnectWizardPage
    {
        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        public ConnectWizardPopupPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override void DoNavigateBack()
        {
            base.DoNavigateBack();

            Parent.CurrentPopupPage = null;
        }

        /// <inheritdoc/>
        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}

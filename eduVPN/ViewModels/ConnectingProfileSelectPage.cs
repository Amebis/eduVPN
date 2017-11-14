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
    public class ConnectingProfileSelectPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Profile select panel
        /// </summary>
        public ConnectingInstanceAndProfileSelectPanel Panel
        {
            get { return _panel; }
            set { SetProperty(ref _panel, value); }
        }
        private ConnectingInstanceAndProfileSelectPanel _panel;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an profile selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public ConnectingProfileSelectPage(ConnectWizard parent) :
            base(parent)
        {
            _panel = new ConnectingInstanceAndProfileSelectPanel(Parent, Parent.InstanceSourceType);
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override void DoNavigateBack()
        {
            base.DoNavigateBack();

            Parent.CurrentPage = Parent.RecentConfigurationSelectPage;
        }

        /// <inheritdoc/>
        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}

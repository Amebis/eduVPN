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

            if (Parent.InstanceSource is Models.LocalInstanceSource)
            {
                switch (Properties.Settings.Default.ConnectingProfileSelectMode)
                {
                    case 0:
                    case 2:
                        if (Parent.InstanceSource.InstanceList.IndexOf(Parent.InstanceSource.AuthenticatingInstance) >= 0)
                            Parent.CurrentPage = Parent.AuthenticatingInstanceSelectPage;
                        else
                            Parent.CurrentPage = Parent.CustomInstancePage;
                        break;

                    case 1:
                        Parent.CurrentPage = Parent.RecentConfigurationSelectPage;
                        break;
                }
            }
            else
                Parent.CurrentPage = Parent.InstanceSourceSelectPage;
        }

        /// <inheritdoc/>
        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}

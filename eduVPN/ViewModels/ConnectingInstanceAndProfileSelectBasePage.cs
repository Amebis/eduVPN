/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Connecting instance and profile selection wizard base page
    /// </summary>
    public class ConnectingInstanceAndProfileSelectBasePage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Profile select panel
        /// </summary>
        public ConnectingInstanceAndProfileSelectPanel Panel
        {
            get { return _panel; }
            set { if (value != _panel) { _panel = value; RaisePropertyChanged(); } }
        }
        private ConnectingInstanceAndProfileSelectPanel _panel;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public ConnectingInstanceAndProfileSelectBasePage(ConnectWizard parent) :
            base(parent)
        {
            Panel = new ConnectingInstanceAndProfileSelectPanel(Parent, Parent.InstanceSourceType);
        }

        #endregion

        #region Methods

        public override void OnActivate()
        {
            base.OnActivate();

            // Set authenticating instance first.
            _panel.AuthenticatingInstance = Parent.AuthenticatingInstance;

            // Force refresh the profile list. Otherwise, failed previous attempt followed by navigating the connection wizard back causes list not to reload.
            _panel.SelectedInstance = null;
            _panel.SelectedInstance = Parent.AuthenticatingInstance;
        }

        protected override void DoNavigateBack()
        {
            base.DoNavigateBack();

            if (Parent.InstanceSource is Models.LocalInstanceSourceInfo)
            {
                if (Parent.InstanceSource.IndexOf(Parent.AuthenticatingInstance) >= 0)
                    Parent.CurrentPage = Parent.AuthenticatingInstanceSelectPage;
                else
                    Parent.CurrentPage = Parent.CustomInstancePage;
            }
            else
                Parent.CurrentPage = Parent.InstanceSourceSelectPage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}

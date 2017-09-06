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
            Panel = new ConnectingInstanceAndProfileSelectPanel(Parent, Parent.InstanceSourceType, Parent.Configuration.AuthenticatingInstance);
        }

        #endregion

        #region Methods

        public override void OnActivate()
        {
            base.OnActivate();

            _panel.OnActivate();
        }

        #endregion
    }
}

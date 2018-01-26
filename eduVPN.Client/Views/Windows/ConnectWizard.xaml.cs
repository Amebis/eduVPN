/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.Client.Windows
{
    /// <summary>
    /// Interaction logic for ConnectWizard.xaml
    /// </summary>
    public partial class ConnectWizard : Views.Windows.ConnectWizard
    {
        #region Properties

        /// <inheritdoc/>
        public override string ClientTitle { get => Client.Resources.Strings.ConnectWizardTitle; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a window
        /// </summary>
        public ConnectWizard()
        {
            InitializeComponent();
        }

        #endregion
    }
}

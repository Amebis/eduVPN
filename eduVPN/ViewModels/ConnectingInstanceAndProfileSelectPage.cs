/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Instance and profile selection wizard page
    /// </summary>
    public class ConnectingInstanceAndProfileSelectPage : ConnectingInstanceAndProfileSelectBasePage
    {
        #region Constructors

        /// <summary>
        /// Constructs a wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public ConnectingInstanceAndProfileSelectPage(ConnectWizard parent) :
            base(parent)
        {
            Panel.SelectedInstance = Parent.AuthenticatingInstance;
        }

        #endregion
    }
}

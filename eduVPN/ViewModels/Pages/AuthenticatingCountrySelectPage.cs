/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Country selection wizard page
    /// </summary>
    public class AuthenticatingCountrySelectPage : AuthenticatingInstanceSelectPage
    {
        #region Properties

        /// <summary>
        /// The page title
        /// </summary>
        public override string Title
        {
            get { return Resources.Strings.AuthenticatingCountrySelectPageTitle; }
        }

        /// <summary>
        /// Instance list label
        /// </summary>
        public override string InstanceListLabel
        {
            get { return Resources.Strings.AuthenticatingCountrySelectPageListLabel; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a country selection wizard page.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public AuthenticatingCountrySelectPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion
    }
}

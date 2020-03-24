/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
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

        /// <inheritdoc/>
        public override string Title
        {
            get { return Resources.Strings.AuthenticatingCountrySelectPageTitle2; }
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

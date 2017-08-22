/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Country selection wizard page
    /// </summary>
    public class CountrySelectPage : InstanceSelectPage
    {
        #region Constructors

        /// <summary>
        /// Constructs a country selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public CountrySelectPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion
    }
}

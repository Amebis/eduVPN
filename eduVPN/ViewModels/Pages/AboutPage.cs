/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;
using System;
using System.Reflection;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// About wizard page
    /// </summary>
    public class AboutPage : ConnectWizardPopupPage
    {
        #region Properties

        /// <summary>
        /// Program version
        /// </summary>
        public Version Version
        {
            get
            {
                return Assembly.GetExecutingAssembly()?.GetName()?.Version;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        public AboutPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion
    }
}

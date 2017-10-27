/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Linq;
using System.Reflection;

namespace eduVPN.ViewModels
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
        public string Version
        {
            get
            {
                return (Attribute.GetCustomAttributes(
                    Assembly.GetExecutingAssembly(),
                    typeof(AssemblyInformationalVersionAttribute)).SingleOrDefault() as AssemblyInformationalVersionAttribute)?.InformationalVersion;
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

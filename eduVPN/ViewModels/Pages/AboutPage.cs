/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
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

        /// <inheritdoc/>
        public override string Title
        {
            get { return Resources.Strings.AboutPageTitle; }
        }

        /// <summary>
        /// Program version
        /// </summary>
        public Version Version
        {
            get
            {
                var ver = Assembly.GetExecutingAssembly()?.GetName()?.Version;
                return
                    ver.Revision != 0 ? new Version(ver.Major, ver.Minor, ver.Build, ver.Revision) :
                    ver.Build    != 0 ? new Version(ver.Major, ver.Minor, ver.Build) :
                                        new Version(ver.Major, ver.Minor);
            }
        }

        /// <summary>
        /// Copyright notice
        /// </summary>
        public string Copyright
        {
            get
            {
                return (Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute)) as AssemblyCopyrightAttribute)?.Copyright;
            }
        }

        /// <summary>
        /// Build timestamp (UTC)
        /// </summary>
        public DateTime Build
        {
            get
            {
                // The Builtin class is implemented in Builtin target in Default.targets.
                return new DateTime(Builtin.CompileTime);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public AboutPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion
    }
}

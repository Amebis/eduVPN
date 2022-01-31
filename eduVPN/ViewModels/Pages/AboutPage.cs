/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
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
                var ver = Assembly.GetExecutingAssembly()?.GetName()?.Version;
                return
                    ver.Revision != 0 ? new Version(ver.Major, ver.Minor, ver.Build, ver.Revision) :
                    ver.Build != 0 ? new Version(ver.Major, ver.Minor, ver.Build) :
                        new Version(ver.Major, ver.Minor);
            }
        }

        /// <summary>
        /// Copyright notice
        /// </summary>
        public string Copyright => (Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute)) as AssemblyCopyrightAttribute)?.Copyright;

        /// <summary>
        /// Build timestamp
        /// </summary>
        public DateTimeOffset Build =>
                // The Builtin class is implemented in Builtin target in Default.targets.
                new DateTimeOffset(Builtin.CompileTime, TimeSpan.Zero);

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

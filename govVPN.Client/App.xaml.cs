/*
    govVPN - The open source VPN

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.Shell;
using System;
using System.Windows;

namespace govVPN.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : eduVPN.Views.App
    {
        #region Methods

        /// <summary>
        /// Main application method
        /// </summary>
        [STAThread]
        public static void Main()
        {
            if (SingleInstance<eduVPN.Views.App>.InitializeAsFirstInstance("org.govvpn.app"))
            {
                try
                {
                    // First instance
                    var application = new App();
                    application.InitializeComponent();
                    application.Run();
                }
                finally
                {
                    // Allow single instance code to perform cleanup operations.
                    SingleInstance<eduVPN.Views.App>.Cleanup();
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnStartup(StartupEventArgs e)
        {
            eduVPN.Properties.SettingsEx.Default.RegistryKeyPath = @"SOFTWARE\SURF\govVPN";
            eduVPN.Views.Properties.SettingsEx.Default.RegistryKeyPath = @"SOFTWARE\SURF\govVPN\Views";

            base.OnStartup(e);

            eduVPN.Properties.Settings.Default.SelfUpdateBundleId = "{84496622-3021-458A-BA35-983507AE8EBC}";
            eduVPN.Properties.Settings.Default.ClientId = "org.govvpn.app";
            eduVPN.Properties.Settings.Default.ClientTitle = "govVPN";
            eduVPN.Properties.Settings.Default.ClientSimpleName = "govVPN";
            eduVPN.Properties.Settings.Default.ClientAboutUri = new Uri(@"https://www.govvpn.org/");
        }

        #endregion
    }
}

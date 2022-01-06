/*
    Let's Connect! - The open source VPN

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.Shell;
using System;
using System.Windows;

namespace LetsConnect.Client
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
            if (SingleInstance<eduVPN.Views.App>.InitializeAsFirstInstance("org.letsconnect-vpn.app"))
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
            eduVPN.Properties.SettingsEx.Default.RegistryKeyPath = @"SOFTWARE\SURF\LetsConnect";
            eduVPN.Views.Properties.SettingsEx.Default.RegistryKeyPath = @"SOFTWARE\SURF\LetsConnect\Views";

            base.OnStartup(e);

            eduVPN.Properties.Settings.Default.SelfUpdateBundleId = "{5F7860D5-5563-4492-930B-C8C77A539504}";
            eduVPN.Properties.Settings.Default.ClientId = "org.letsconnect-vpn.app";
            eduVPN.Properties.Settings.Default.ClientTitle = "Let's Connect!";
            eduVPN.Properties.Settings.Default.ClientAboutUri = new Uri(@"https://www.letsconnect-vpn.org/");
        }

        #endregion
    }
}

/*
    Let's Connect! - The open source VPN

    Copyright: 2017-2023 The Commons Conservancy
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
        }

        #endregion
    }
}

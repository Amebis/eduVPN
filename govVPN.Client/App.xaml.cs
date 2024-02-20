/*
    govVPN - The open source VPN

    Copyright: 2017-2024 The Commons Conservancy
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
        }

        #endregion
    }
}

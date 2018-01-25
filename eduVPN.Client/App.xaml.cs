/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.Shell;
using System;
using System.Windows;

namespace eduVPN.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Views.Application
    {
        #region Methods

        /// <summary>
        /// Main application method
        /// </summary>
        [STAThread]
        public static void Main()
        {
            if (SingleInstance<Views.Application>.InitializeAsFirstInstance("org.eduvpn.app"))
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
                    SingleInstance<Views.Application>.Cleanup();
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (Client.Properties.Settings.Default.SettingsVersion == 0)
            {
                // Migrate settings from previous version.
                Client.Properties.Settings.Default.Upgrade();
                Client.Properties.Settings.Default.SettingsVersion = 1;
            }
        }


        /// <inheritdoc/>
        protected override void OnExit(ExitEventArgs e)
        {
            Client.Properties.Settings.Default.Save();

            base.OnExit(e);
        }

        #endregion
    }
}

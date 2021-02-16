/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
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
    public partial class App : Views.App
    {
        #region Methods

        /// <summary>
        /// Main application method
        /// </summary>
        [STAThread]
        public static void Main()
        {
            if (SingleInstance<Views.App>.InitializeAsFirstInstance("org.eduvpn.app"))
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
                    SingleInstance<Views.App>.Cleanup();
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Set client-specific settings.
            eduVPN.Properties.Settings.Default.SelfUpdateBundleId = "{EF5D5806-B90B-4AA3-800A-2D7EA1592BA0}";
            eduVPN.Properties.Settings.Default.ClientId = "org.eduvpn.app";
            eduVPN.Properties.Settings.Default.ClientTitle = Client.Resources.Strings.ConnectWizardTitle;
            eduVPN.Properties.Settings.Default.ClientAboutUri = new Uri(Client.Resources.Strings.AboutPageUri);
        }

        #endregion
    }
}

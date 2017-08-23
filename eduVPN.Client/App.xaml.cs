/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.Shell;
using System;
using System.Collections.Generic;
using System.Net;
//using System.Threading;
using System.Windows;

namespace eduVPN.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        #region Constructors

        /// <summary>
        /// Constructs the application
        /// </summary>
        public App()
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Main application method
        /// </summary>
        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance("org.eduvpn.app"))
            {
                if (eduVPN.Client.Properties.Settings.Default.SettingsVersion == 0)
                {
                    // Migrate settings from previous version.
                    eduVPN.Client.Properties.Settings.Default.Upgrade();
                    eduVPN.Client.Properties.Settings.Default.SettingsVersion = 1;
                }

                // First instance
                var application = new App();
                application.InitializeComponent();
                application.Run();

                // Allow single instance code to perform cleanup operations.
                SingleInstance<App>.Cleanup();
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // .NET 3.5 allows Schannel to use SSL 3 and TLS 1.0 by default. Instead of hacking user computer's registry, extend it in runtime.
            // System.Net.SecurityProtocolType lacks appropriate constants prior to .NET 4.5.
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)0x0C00;

            //// Set language preference.
            //var culture = new CultureInfo("sl-SI");
            //CultureInfo.DefaultThreadCurrentCulture = culture;
            //CultureInfo.DefaultThreadCurrentUICulture = culture;
            //Thread.CurrentThread.CurrentCulture = culture;
            //Thread.CurrentThread.CurrentUICulture = culture;

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            eduVPN.Client.Properties.Settings.Default.Save();

            base.OnExit(e);
        }

        #endregion

        #region ISingleInstanceApp Implementation

        /// <summary>
        /// Handles secondary application instance invocation.
        /// </summary>
        /// <param name="args">Command line parameters</param>
        /// <returns><c>true</c></returns>
        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            if (args.Count >= 2)
            {
                // Forward redirect URI to the wizard.
                var uri = args[1];
                var wizard = (ViewModels.ConnectWizard)(((Views.ConnectWizard)MainWindow).DataContext);

                if (wizard.AuthorizationPage.Authorize.CanExecute(uri))
                {
                    wizard.AuthorizationPage.Authorize.Execute(uri);

                    // (Re)activate window.
                    if (!MainWindow.IsActive)
                        MainWindow.Show();
                    MainWindow.Activate();
                    MainWindow.Focus();
                }
            }

            return true;
        }

        #endregion
    }
}

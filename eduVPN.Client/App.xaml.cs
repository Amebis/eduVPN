/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.Shell;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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
            if (!SingleInstance<App>.InitializeAsFirstInstance("org.eduvpn.app"))
            {
                // This is not the first instance. Quit.
                return;
            }

            try
            {
                try
                {
                    // Test load the user settings.
                    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                }
                catch (ConfigurationErrorsException ex)
                {
                    // Ups, something is wrong with the user settings.
                    var assembly_title = (Attribute.GetCustomAttributes(Assembly.GetExecutingAssembly(), typeof(AssemblyTitleAttribute)).SingleOrDefault() as AssemblyTitleAttribute)?.Title;
                    var filename = ex.Filename;
                    if (MessageBox.Show(
                        string.Format(eduVPN.Client.Resources.Strings.SettingsCorruptErrorMessage, assembly_title, filename),
                        eduVPN.Client.Resources.Strings.SettingsCorruptErrorTitle,
                        MessageBoxButton.OKCancel,
                        MessageBoxImage.Error) == MessageBoxResult.OK)
                    {
                        // Delete user settings file and continue.
                        File.Delete(filename);
                    }
                    else
                    {
                        // User cancelled. Quit.
                        return;
                    }
                }

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
            }
            finally
            {
                // Allow single instance code to perform cleanup operations.
                SingleInstance<App>.Cleanup();
            }
        }

        /// <inheritdoc/>
        protected override void OnStartup(StartupEventArgs e)
        {
            // .NET 3.5 allows Schannel to use SSL 3 and TLS 1.0 by default. Instead of hacking user computer's registry, extend it in runtime.
            // System.Net.SecurityProtocolType lacks appropriate constants prior to .NET 4.5.
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)0x0C00;

            //// Set language preference.
            //var culture = new System.Globalization.CultureInfo("sl-SI");
            //System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
            //System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
            //System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            //System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

            base.OnStartup(e);
        }

        /// <inheritdoc/>
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
            // (Re)activate main window.
            if (!MainWindow.IsActive)
                MainWindow.Show();
            MainWindow.Activate();
            MainWindow.Focus();

            if (args.Count >= 2)
            {
                // Forward redirect URI to the authorization pop-up.
                var uri = args[1];
                var wizard = (eduVPN.Views.ConnectWizard)MainWindow;

                if (wizard.AuthorizationPopup != null &&
                    wizard.AuthorizationPopup.DataContext is ViewModels.AuthorizationPopup view_model &&
                    view_model.Authorize.CanExecute(uri))
                    view_model.Authorize.Execute(uri);
            }

            return true;
        }

        #endregion
    }
}

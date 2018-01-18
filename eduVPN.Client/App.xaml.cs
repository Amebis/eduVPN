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
        #region Methods

        /// <summary>
        /// Main application method
        /// </summary>
        [STAThread]
        public static void Main()
        {
            // If app.local.config file is present, read configuration from it.
            var app_config_file = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            var local_config_file = Path.Combine(Path.GetDirectoryName(app_config_file), Path.GetFileNameWithoutExtension(app_config_file) + ".local.config");
            if (File.Exists(local_config_file))
            {
                AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", local_config_file);

                typeof(ConfigurationManager)
                    .GetField("s_initState", BindingFlags.NonPublic | BindingFlags.Static)
                    .SetValue(null, 0);

                typeof(ConfigurationManager)
                    .GetField("s_configSystem", BindingFlags.NonPublic | BindingFlags.Static)
                    .SetValue(null, null);

                typeof(ConfigurationManager)
                    .Assembly.GetTypes()
                    .Where(x => x.FullName == "System.Configuration.ClientConfigPaths")
                    .First()
                    .GetField("s_current", BindingFlags.NonPublic | BindingFlags.Static)
                    .SetValue(null, null);
            }

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
                        string.Format(Client.Resources.Strings.SettingsCorruptErrorMessage, assembly_title, filename),
                        Client.Resources.Strings.SettingsCorruptErrorTitle,
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

                Global.Initialize();

                if (Client.Properties.Settings.Default.SettingsVersion == 0)
                {
                    // Migrate settings from previous version.
                    Client.Properties.Settings.Default.Upgrade();
                    Client.Properties.Settings.Default.SettingsVersion = 1;
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

            // Client is using parallelism in web data retrieval.
            // The default maximum concurrent connections set to 2 is a bottleneck.
            ServicePointManager.DefaultConnectionLimit = 5;

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
            Client.Properties.Settings.Default.Save();
            eduVPN.Properties.Settings.Default.Save();

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
            MainWindow.Topmost = true;
            try
            {
                MainWindow.Activate();
                MainWindow.Focus();
            }
            finally
            {
                MainWindow.Topmost = false;
            }

            return true;
        }

        #endregion
    }
}

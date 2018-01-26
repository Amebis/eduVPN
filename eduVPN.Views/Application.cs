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

namespace eduVPN.Views
{
    /// <summary>
    /// View application base class
    /// </summary>
    public class Application : System.Windows.Application, ISingleInstanceApp
    {
        #region Methods

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
                    String.Format(Views.Resources.Strings.SettingsCorruptErrorMessage, assembly_title, filename),
                    Views.Resources.Strings.SettingsCorruptErrorTitle,
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

            eduVPN.Properties.Settings.Initialize();

            base.OnStartup(e);
        }

        /// <inheritdoc/>
        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            base.OnSessionEnding(e);

            // Save view model settings on logout.
            eduVPN.Properties.Settings.Default.Save();
        }

        /// <inheritdoc/>
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            // Save view model settings on exit.
            eduVPN.Properties.Settings.Default.Save();
        }

        #endregion

        #region ISingleInstanceApp Implementation

        /// <summary>
        /// Handles secondary application instance invocation.
        /// </summary>
        /// <param name="args">Command line parameters</param>
        public void SignalExternalCommandLineArgs(IList<string> args)
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
        }

        #endregion
    }
}

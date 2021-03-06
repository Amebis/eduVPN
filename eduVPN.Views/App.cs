﻿/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
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
using System.Windows.Media;

namespace eduVPN.Views
{
    /// <summary>
    /// View application base class
    /// </summary>
    public class App : Application, ISingleInstanceApp
    {
        #region Properties

        /// <summary>
        /// Was client started at user sign-on?
        /// </summary>
        public bool IsSignon { get; private set; }

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override void OnStartup(StartupEventArgs e)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            // Client is using parallelism in web data retrieval.
            // The default maximum concurrent connections set to 2 is a bottleneck.
            ServicePointManager.DefaultConnectionLimit = 5;

            //// Set language preference.
            //var culture = new System.Globalization.CultureInfo("sl-SI");
            //var culture = new System.Globalization.CultureInfo("ar-MA");
            //var culture = new System.Globalization.CultureInfo("tr-TR");
            //var culture = new System.Globalization.CultureInfo("es-CL");
            //System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
            //System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
            //System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            //System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

            try
            {
                // Test load the user settings.
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            }
            catch (ConfigurationErrorsException ex)
            {
                // Ups, something is wrong with the user settings.
                var assemblyTitle = (Attribute.GetCustomAttributes(Assembly.GetExecutingAssembly(), typeof(AssemblyTitleAttribute)).SingleOrDefault() as AssemblyTitleAttribute)?.Title;
                var filename = ex.Filename;
                if (MessageBox.Show(
                    string.Format(Views.Resources.Strings.SettingsCorruptErrorMessage, assemblyTitle, filename),
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
                    Shutdown(1);
                }
            }

            eduVPN.Properties.Settings.Initialize();
            Views.Properties.Settings.Initialize();

            IsSignon = e.Args.Any(param => param.Equals("/signon", StringComparison.OrdinalIgnoreCase));
            if (IsSignon && !Views.Properties.Settings.Default.StartOnSignon)
                Shutdown(0);

            RenderOptions.ProcessRenderMode = Views.Properties.SettingsEx.Default.ProcessRenderMode;

            base.OnStartup(e);
        }

        /// <inheritdoc/>
        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            base.OnSessionEnding(e);

            // Save view settings on logout.
            Views.Properties.Settings.Default.Save();

            // Save view model settings on logout.
            eduVPN.Properties.Settings.Default.Save();
        }

        /// <inheritdoc/>
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            // Save view settings on exit.
            Views.Properties.Settings.Default.Save();

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

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.Shell;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace eduVPN.Views
{
    /// <summary>
    /// View application base class
    /// </summary>
    public class App : Application, ISingleInstanceApp
    {
        #region Fields

        StandardStreamTracer StandardOutputTracer;

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override void OnStartup(StartupEventArgs e)
        {
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
                    return;
                }
            }

            eduVPN.Properties.Settings.Initialize();
            Views.Properties.Settings.Initialize();

            StandardOutputTracer = new StandardStreamTracer();

            eduVPN.Properties.Settings.Default.IsSignon = e.Args.Any(param => param.Equals("/signon", StringComparison.OrdinalIgnoreCase));
            if (eduVPN.Properties.Settings.Default.IsSignon && !Views.Properties.SettingsEx.Default.StartOnSignon)
                Shutdown(0);

            RenderOptions.ProcessRenderMode = Views.Properties.SettingsEx.Default.ProcessRenderMode;

            foreach (var settings in new ApplicationSettingsBase[] { eduVPN.Properties.Settings.Default, Views.Properties.Settings.Default })
            {
                var timer = new Timer(5 * 1000) { AutoReset = false };
                timer.Elapsed += (object sender, ElapsedEventArgs e2) =>
                {
                    if (!Dispatcher.HasShutdownStarted)
                        Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                        {
                            Trace.TraceInformation("Saving user settings after change");
                            settings.Save();
                        }));
                };
                settings.PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e2) =>
                {
                    timer.Stop();
                    timer.Start();
                };
            }

            base.OnStartup(e);
        }

        /// <inheritdoc/>
        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            var mainWindow = MainWindow as Windows.ConnectWizard;
            var activeSession = (mainWindow.DataContext as ViewModels.Windows.ConnectWizard).ConnectionPage.ActiveSession;
            if (activeSession != null)
            {
                // If our app has no window open, it will get killed. Restore the main window first, then do a standard app quit.
                mainWindow.Open_Click(this, null);
                mainWindow.Exit_Click(this, null);

                // Deny session ending so we can complete the /disconnect notification.
                e.Cancel = true;
            }
            else
            {
                base.OnSessionEnding(e);

                // Save settings on logout.
                Trace.TraceInformation("Saving user settings on session ending");
                Views.Properties.Settings.Default.Save();
                eduVPN.Properties.Settings.Default.Save();

                StandardOutputTracer.Dispose();
            }
        }

        /// <inheritdoc/>
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            // Save settings on exit.
            Trace.TraceInformation("Saving user settings on client exit");
            Views.Properties.Settings.Default.Save();
            eduVPN.Properties.Settings.Default.Save();

            StandardOutputTracer.Dispose();
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

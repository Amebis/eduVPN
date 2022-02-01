/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduEx.System;
using eduVPN.ViewModels.Windows;
using Microsoft.Win32;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Prompts user to make the self-update choice.
    /// </summary>
    public class SelfUpdatePromptPage : ConnectWizardPopupPage
    {
        #region Properties

        /// <summary>
        /// Installed product version
        /// </summary>
        public Version InstalledVersion
        {
            get => _InstalledVersion;
            private set => SetProperty(ref _InstalledVersion, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Version _InstalledVersion;

        /// <summary>
        /// Available product version
        /// </summary>
        public Version AvailableVersion
        {
            get => _AvailableVersion;
            private set
            {
                if (SetProperty(ref _AvailableVersion, value))
                {
                    _StartUpdate?.RaiseCanExecuteChanged();
                    _SkipUpdate?.RaiseCanExecuteChanged();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Version _AvailableVersion;

        /// <summary>
        /// Product changelog
        /// </summary>
        public Uri Changelog
        {
            get => _Changelog;
            private set
            {
                if (SetProperty(ref _Changelog, value))
                    _ShowChangelog?.RaiseCanExecuteChanged();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Uri _Changelog;

        /// <summary>
        /// Pass control to the self-update page
        /// </summary>
        public DelegateCommand StartUpdate
        {
            get
            {
                if (_StartUpdate == null)
                    _StartUpdate = new DelegateCommand(
                        () =>
                        {
                            if (Wizard.NavigateTo.CanExecute(Wizard.SelfUpdateProgressPage))
                                Wizard.NavigateTo.Execute(Wizard.SelfUpdateProgressPage);
                        },
                        () => AvailableVersion != null);
                return _StartUpdate;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _StartUpdate;

        /// <summary>
        /// Mark not to re-prompt again
        /// </summary>
        public DelegateCommand SkipUpdate
        {
            get
            {
                if (_SkipUpdate == null)
                    _SkipUpdate = new DelegateCommand(
                        () =>
                        {
                            Trace.TraceInformation("User choose to skip this update.");
                            Properties.Settings.Default.SelfUpdateLastReminder = DateTimeOffset.MaxValue;
                            if (NavigateBack.CanExecute())
                                NavigateBack.Execute();
                        },
                        () => AvailableVersion != null);
                return _SkipUpdate;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _SkipUpdate;

        /// <summary>
        /// Show changelog command
        /// </summary>
        public DelegateCommand ShowChangelog
        {
            get
            {
                if (_ShowChangelog == null)
                    _ShowChangelog = new DelegateCommand(
                        () => Process.Start(Changelog.ToString()),
                        () => Changelog != null);
                return _ShowChangelog;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ShowChangelog;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public SelfUpdatePromptPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks for installed product version and published version and offers user to update
        /// </summary>
        public void CheckForUpdates()
        {
            try
            {
                Parallel.ForEach(new List<Action>()
                    {
                        () =>
                        {
                            // Get self-update.
                            var res = Properties.SettingsEx.Default.SelfUpdateDiscovery;
                            Trace.TraceInformation("Downloading self-update JSON discovery from {0}...", res.Uri.AbsoluteUri);
                            var obj = Properties.Settings.Default.ResponseCache.GetSeq(res, Window.Abort.Token);

                            var repoVersion = new Version((string)obj["version"]);
                            Trace.TraceInformation("Online version: {0}", repoVersion.ToString());
                            Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => {
                                AvailableVersion = repoVersion;
                                Changelog = eduJSON.Parser.GetValue(obj, "changelog_uri", out string changelogUri) ? new Uri(changelogUri) : null;
                                Wizard.SelfUpdateProgressPage.DownloadUris = new List<Uri>(((List<object>)obj["uri"]).Select(uri => new Uri(res.Uri, (string)uri)));
                                Wizard.SelfUpdateProgressPage.Hash = ((string)obj["hash-sha256"]).FromHexToBin();
                                Wizard.SelfUpdateProgressPage.Arguments = eduJSON.Parser.GetValue(obj, "arguments", out string installerArguments) ? installerArguments : null;
                            }));
                        },

                        () =>
                        {
                            // Evaluate installed products.
                            Version productVersion = null;
                            var productId = Properties.Settings.Default.SelfUpdateBundleId.ToUpperInvariant();
                            Trace.TraceInformation("Evaluating installed products...");
                            using (var hklmKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                            using (var uninstallKey = hklmKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", false))
                            {
                                foreach (var productKeyName in uninstallKey.GetSubKeyNames())
                                {
                                    Window.Abort.Token.ThrowIfCancellationRequested();
                                    using (var productKey = uninstallKey.OpenSubKey(productKeyName))
                                    {
                                        var bundleUpgradeCode = productKey.GetValue("BundleUpgradeCode");
                                        if ((bundleUpgradeCode is string   bundleUpgradeCodeString && bundleUpgradeCodeString.ToUpperInvariant() == productId ||
                                                bundleUpgradeCode is string[] bundleUpgradeCodeArray  && bundleUpgradeCodeArray.FirstOrDefault(code => code.ToUpperInvariant() == productId) != null) &&
                                            productKey.GetValue("BundleVersion") is string bundleVersionString)
                                        {
                                            // Our product entry found.
                                            productVersion = new Version(productKey.GetValue("DisplayVersion") is string displayVersionString ? displayVersionString : bundleVersionString);
                                            Trace.TraceInformation("Installed version: {0}", productVersion.ToString());
                                            break;
                                        }
                                    }
                                }
                            }
                            Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { InstalledVersion = productVersion; }));
                        },
                    },
                    action =>
                    {
                        Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount++));
                        try { action(); }
                        finally { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount--)); }
                    });
            }
            catch (AggregateException ex)
            {
                var nonCancelledException = ex.InnerExceptions.Where(innerException => !(innerException is OperationCanceledException));
                if (nonCancelledException.Any())
                    throw new AggregateException(Resources.Strings.ErrorSelfUpdateDetection, nonCancelledException.ToArray());
                throw new OperationCanceledException();
            }

            //// Mock the values for testing.
            //InstalledVersion = new Version(1, 0);
            //Properties.Settings.Default.SelfUpdateLastReminder = DateTimeOffset.MinValue;

            try
            {
                if (new Version(Properties.Settings.Default.SelfUpdateLastVersion) == AvailableVersion &&
                    (Properties.Settings.Default.SelfUpdateLastReminder == DateTimeOffset.MaxValue ||
                    (DateTimeOffset.Now - Properties.Settings.Default.SelfUpdateLastReminder).TotalDays < 3))
                {
                    // We already prompted user for this version.
                    // Either user opted not to be reminded of this version update again,
                    // or it has been less than three days since the last prompt.
                    Trace.TraceInformation("Update deferred by user choice.");
                    return;
                }
            }
            catch { }

            if (InstalledVersion == null)
            {
                // Nothing to update.
                Trace.TraceInformation("Product not installed or version could not be determined.");
                return; // Quit self-updating.
            }

            if (AvailableVersion <= InstalledVersion)
            {
                // Product already up-to-date.
                Trace.TraceInformation("Update not required.");
                return;
            }

            // We're in the background thread - raise the prompt event via dispatcher.
            Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
            {
                if (Wizard.NavigateTo.CanExecute(this))
                {
                    Properties.Settings.Default.SelfUpdateLastVersion = AvailableVersion.ToString();
                    Properties.Settings.Default.SelfUpdateLastReminder = DateTimeOffset.Now;
                    Wizard.NavigateTo.Execute(this);
                }
            }));
        }

        #endregion
    }
}

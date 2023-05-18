/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.Diagnostics;

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
            private set
            {
                if (SetProperty(ref _InstalledVersion, value))
                    _SkipUpdate?.RaiseCanExecuteChanged();
            }
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
                        () => AvailableVersion != null && InstalledVersion != null && AvailableVersion > InstalledVersion);
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
                {
                    _SkipUpdate = new DelegateCommand(
                        () =>
                        {
                            Trace.TraceInformation("User choose to skip this update");
                            Properties.Settings.Default.SelfUpdateLastReminder = DateTimeOffset.MaxValue;
                            if (NavigateBack.CanExecute())
                                NavigateBack.Execute();
                        },
                        () => AvailableVersion != null);
                }
                return _SkipUpdate;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _SkipUpdate;

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

        public void DiscoverVersions()
        {
            var r = CGo.CheckSelfUpdate(
                Properties.SettingsEx.Default.SelfUpdateDiscovery,
                Properties.Settings.Default.SelfUpdateBundleId,
                Window.Abort.Token);
            Wizard.TryInvoke((Action)(() =>
            {
                if (r.Item1 != null)
                {
                    AvailableVersion = r.Item1.Version;
                    Changelog = r.Item1.Changelog;
                    Wizard.SelfUpdateProgressPage.DownloadUris = r.Item1.Uris;
                    Wizard.SelfUpdateProgressPage.Hash = r.Item1.Hash;
                    Wizard.SelfUpdateProgressPage.Arguments = r.Item1.Arguments;
                }
                else
                {
                    AvailableVersion = null;
                    Changelog = null;
                    Wizard.SelfUpdateProgressPage.DownloadUris = null;
                    Wizard.SelfUpdateProgressPage.Hash = null;
                    Wizard.SelfUpdateProgressPage.Arguments = null;
                }

                InstalledVersion = r.Item2 ?? null;
            }));

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
                    Trace.TraceInformation("Update deferred by user choice");
                    return;
                }
            }
            catch { }

            if (InstalledVersion == null)
            {
                // Nothing to update.
                Trace.TraceInformation("Product not installed or version could not be determined");
                return; // Quit self-updating.
            }

            if (AvailableVersion <= InstalledVersion)
            {
                // Product already up-to-date.
                Trace.TraceInformation("Update not required");
                return;
            }

            // We're in the background thread - raise the prompt event via dispatcher.
            Wizard.TryInvoke((Action)(() =>
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

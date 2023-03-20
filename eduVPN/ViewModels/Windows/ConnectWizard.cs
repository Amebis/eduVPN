/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduEx.System;
using eduVPN.Models;
using eduVPN.ViewModels.Pages;
using Microsoft.Win32;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace eduVPN.ViewModels.Windows
{
    /// <summary>
    /// Connect wizard
    /// </summary>
    public class ConnectWizard : Window
    {
        #region Fields

        /// <summary>
        /// Stack of displayed popup pages
        /// </summary>
        private readonly List<ConnectWizardPopupPage> PopupPages = new List<ConnectWizardPopupPage>();

        #endregion

        #region Properties

        /// <summary>
        /// Copyright notice
        /// </summary>
        public string Copyright => (Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute)) as AssemblyCopyrightAttribute)?.Copyright;

        /// <summary>
        /// Executing assembly version
        /// </summary>
        public Version Version
        {
            get
            {
                var ver = Assembly.GetExecutingAssembly()?.GetName()?.Version;
                return
                    ver.Revision != 0 ? new Version(ver.Major, ver.Minor, ver.Build, ver.Revision) :
                    ver.Build != 0 ? new Version(ver.Major, ver.Minor, ver.Build) :
                        new Version(ver.Major, ver.Minor);
            }
        }

        /// <summary>
        /// Build timestamp
        /// </summary>
        public DateTimeOffset Build =>
                // The Builtin class is implemented in Builtin target in Default.targets.
                new DateTimeOffset(Builtin.CompileTime, TimeSpan.Zero);

        /// <summary>
        /// Installed product version
        /// </summary>
        public Version InstalledVersion
        {
            get => _InstalledVersion;
            private set
            {
                if (SetProperty(ref _InstalledVersion, value))
                    _StartUpdate?.RaiseCanExecuteChanged();
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
                    _StartUpdate?.RaiseCanExecuteChanged();
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
                            if (NavigateTo.CanExecute(SelfUpdateProgressPage))
                                NavigateTo.Execute(SelfUpdateProgressPage);
                        },
                        () => AvailableVersion != null && InstalledVersion != null && AvailableVersion > InstalledVersion);
                return _StartUpdate;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _StartUpdate;

        /// <summary>
        /// Navigate to a pop-up page command
        /// </summary>
        public DelegateCommand<ConnectWizardPopupPage> NavigateTo
        {
            get
            {
                if (_NavigateTo == null)
                    _NavigateTo = new DelegateCommand<ConnectWizardPopupPage>(
                        page =>
                        {
                            var displayPagePrev = DisplayPage;
                            var removed = PopupPages.Remove(page);
                            PopupPages.Add(page);
                            if (!removed) page.OnActivate();
                            if (displayPagePrev != DisplayPage)
                                RaisePropertyChanged(nameof(DisplayPage));
                        });
                return _NavigateTo;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand<ConnectWizardPopupPage> _NavigateTo;

        /// <summary>
        /// Navigate back from a pop-up page command
        /// </summary>
        public DelegateCommand<ConnectWizardPopupPage> NavigateBack
        {
            get
            {
                if (_NavigateBack == null)
                    _NavigateBack = new DelegateCommand<ConnectWizardPopupPage>(
                        page =>
                        {
                            var displayPagePrev = DisplayPage;
                            PopupPages.Remove(page);
                            if (displayPagePrev != DisplayPage)
                                RaisePropertyChanged(nameof(DisplayPage));
                        });
                return _NavigateBack;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand<ConnectWizardPopupPage> _NavigateBack;

        /// <summary>
        /// Occurs when auto-reconnection failed.
        /// </summary>
        /// <remarks>Sender is the connection wizard <see cref="ConnectWizard"/>.</remarks>
        public event EventHandler<AutoReconnectFailedEventArgs> AutoReconnectFailed;

        /// <summary>
        /// Occurs when application should quit.
        /// </summary>
        /// <remarks>Sender is the connection wizard <see cref="ConnectWizard"/>.</remarks>
        public event EventHandler QuitApplication;

        #region Pages

        /// <summary>
        /// The page the wizard is currently displaying
        /// </summary>
        public ConnectWizardPage DisplayPage => PopupPages.Count > 0 ? (ConnectWizardPage)PopupPages.Last() : _CurrentPage;

        /// <summary>
        /// The page the wizard should be displaying (if no pop-up page)
        /// </summary>
        public ConnectWizardStandardPage CurrentPage
        {
            get => _CurrentPage;
            set
            {
                if (SetProperty(ref _CurrentPage, value))
                {
                    _CurrentPage.OnActivate();
                    if (PopupPages.Count <= 0)
                        RaisePropertyChanged(nameof(DisplayPage));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ConnectWizardStandardPage _CurrentPage;

        /// <summary>
        /// Page to add another server
        /// </summary>
        public ConnectWizardStandardPage AddAnotherPage => Properties.Settings.Default.Discovery ? (ConnectWizardStandardPage)SearchPage : SelectOwnServerPage;

        /// <summary>
        /// The first page of the wizard
        /// </summary>
        public ConnectWizardStandardPage StartingPage
        {
            get
            {
                if (HomePage.InstituteAccessServers.Count != 0 ||
                    HomePage.SecureInternetServers.Count != 0 ||
                    HomePage.OwnServers.Count != 0)
                    return HomePage;

                return AddAnotherPage;
            }
        }

        /// <summary>
        /// Authorization wizard page
        /// </summary>
        public AuthorizationPage AuthorizationPage
        {
            get
            {
                if (_AuthorizationPage == null)
                    _AuthorizationPage = new AuthorizationPage(this);
                return _AuthorizationPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private AuthorizationPage _AuthorizationPage;

        /// <summary>
        /// Search wizard page
        /// </summary>
        public SearchPage SearchPage
        {
            get
            {
                if (_SearchPage == null)
                    _SearchPage = new SearchPage(this);
                return _SearchPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SearchPage _SearchPage;

        /// <summary>
        /// Select own server page
        /// </summary>
        public SelectOwnServerPage SelectOwnServerPage
        {
            get
            {
                if (_SelectOwnServerPage == null)
                    _SelectOwnServerPage = new SelectOwnServerPage(this);
                return _SelectOwnServerPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SelectOwnServerPage _SelectOwnServerPage;

        /// <summary>
        /// Home wizard page
        /// </summary>
        public HomePage HomePage
        {
            get
            {
                if (_HomePage == null)
                    _HomePage = new HomePage(this);
                return _HomePage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private HomePage _HomePage;

        /// <summary>
        /// Select secure internet connecting server wizard page
        /// </summary>
        public SelectSecureInternetCountryPage SelectSecureInternetCountryPage
        {
            get
            {
                if (_SelectSecureInternetCountryPage == null)
                    _SelectSecureInternetCountryPage = new SelectSecureInternetCountryPage(this);
                return _SelectSecureInternetCountryPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SelectSecureInternetCountryPage _SelectSecureInternetCountryPage;

        /// <summary>
        /// Status wizard page
        /// </summary>
        public ConnectionPage ConnectionPage
        {
            get
            {
                if (_ConnectionPage == null)
                    _ConnectionPage = new ConnectionPage(this);
                return _ConnectionPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ConnectionPage _ConnectionPage;

        /// <summary>
        /// Settings wizard page
        /// </summary>
        public SettingsPage SettingsPage
        {
            get
            {
                if (_SettingsPage == null)
                    _SettingsPage = new SettingsPage(this);
                return _SettingsPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SettingsPage _SettingsPage;

        /// <summary>
        /// About wizard page
        /// </summary>
        public AboutPage AboutPage
        {
            get
            {
                if (_AboutPage == null)
                    _AboutPage = new AboutPage(this);
                return _AboutPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private AboutPage _AboutPage;

        /// <summary>
        /// Self-update prompt wizard page
        /// </summary>
        public SelfUpdatePromptPage SelfUpdatePromptPage
        {
            get
            {
                if (_SelfUpdatePromptPage == null)
                    _SelfUpdatePromptPage = new SelfUpdatePromptPage(this);
                return _SelfUpdatePromptPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SelfUpdatePromptPage _SelfUpdatePromptPage;

        /// <summary>
        /// Self-update progress wizard page
        /// </summary>
        public SelfUpdateProgressPage SelfUpdateProgressPage
        {
            get
            {
                if (_SelfUpdateProgressPage == null)
                    _SelfUpdateProgressPage = new SelfUpdateProgressPage(this);
                return _SelfUpdateProgressPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SelfUpdateProgressPage _SelfUpdateProgressPage;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs the wizard
        /// </summary>
        public ConnectWizard()
        {
            Engine.Callback += Engine_Callback;

            var actions = new List<KeyValuePair<Action, int>>();

            actions.Add(new KeyValuePair<Action, int>(
                () =>
                {
                    try { Properties.Settings.Default.ResponseCache.PurgeOldCacheEntries(); } catch { }
                    try { VPN.OpenVPNSession.PurgeOldLogs(); } catch { }
                    try { VPN.WireGuardSession.PurgeOldLogs(); } catch { }
                },
                24 * 60 * 60 * 1000)); // Repeat every 24 hours

            // TODO: Migrate eduVPN settings to eduvpn-common.
            // TODO: Support preconfigured Institute Access and Secure Internet to eduvpn-common.
            //var str = Engine.ServerList();
            //foreach (var srv in Properties.Settings.Default.InstituteAccessServers)
            //    Engine.AddInstituteAccessServer(srv);
            //if (!string.IsNullOrEmpty(Properties.Settings.Default.SecureInternetOrganization))
            //    Engine.AddSecureInternetHomeServer(Properties.Settings.Default.SecureInternetOrganization);
            //foreach (var srv in Properties.Settings.Default.OwnServers)
            //    Engine.AddOwnServer(srv);
            //str = Engine.ServerList();

            if (Properties.SettingsEx.Default.SelfUpdateDiscovery?.Uri != null)
                actions.Add(new KeyValuePair<Action, int>(
                    DiscoverVersions,
                    24 * 60 * 60 * 1000)); // Repeat every 24 hours

            // Show Starting wizard page.
            CurrentPage = StartingPage;

            foreach (var action in actions)
            {
                var w = new BackgroundWorker();
                w.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    var random = new Random();
                    do
                    {
                        TryInvoke((Action)(() => TaskCount++));
                        try { action.Key(); }
                        catch (OperationCanceledException) { }
                        catch (Exception ex) { TryInvoke((Action)(() => throw ex)); }
                        finally { TryInvoke((Action)(() => TaskCount--)); }
                    }
                    // Sleep for given time±10%, then retry.
                    while (action.Value != 0 && !Abort.Token.WaitHandle.WaitOne(random.Next(action.Value * 9 / 10, action.Value * 11 / 10)));
                };
                w.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) => (sender as BackgroundWorker)?.Dispose();
                w.RunWorkerAsync();
            }

            //Abort.Token.WaitHandle.WaitOne(5000); // Mock slow wizard initialization
        }

        #endregion

        #region Methods

        private void Engine_Callback(object sender, Engine.CallbackEventArgs e)
        {
            switch (e.NewState)
            {
                case Engine.State.NoServer:
                    TryInvoke((Action)(() =>
                    {
                        HomePage.LoadServers();
                        CurrentPage = StartingPage;
                    }));
                    e.Handled = true;
                    break;

                case Engine.State.AskLocation:
                    var countries = eduJSON.Parser.Parse(e.Data, Abort.Token) as List<object>;
                    TryInvoke((Action)(() =>
                    {
                        SelectSecureInternetCountryPage.SetSecureInternetCountries(countries);
                        CurrentPage = SelectSecureInternetCountryPage;
                    }));
                    e.Handled = true;
                    break;

                case Engine.State.OAuthStarted:
                    Process.Start(e.Data);
                    TryInvoke((Action)(() => CurrentPage = AuthorizationPage));
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Returns authenticating server for the given connecting server
        /// </summary>
        /// <param name="connectingServer">Connecting server</param>
        /// <returns>Authenticating server</returns>
        [Obsolete]
        public Server GetAuthenticatingServer(Server connectingServer)
        {
            throw new NotImplementedException();
        }

        private void DiscoverVersions()
        {
            try
            {
                Parallel.ForEach(new List<Action>()
                    {
                        () =>
                        {
                            // Get self-update.
                            var res = Properties.SettingsEx.Default.SelfUpdateDiscovery;
                            Trace.TraceInformation("Downloading self-update JSON discovery {0}", res.Uri.AbsoluteUri);
                            var obj = Properties.Settings.Default.ResponseCache.GetSeq(res, Abort.Token);

                            var repoVersion = new Version((string)obj["version"]);
                            Trace.TraceInformation("Online version: {0}", repoVersion.ToString());
                            TryInvoke((Action)(() => {
                                AvailableVersion = repoVersion;
                                Changelog = eduJSON.Parser.GetValue(obj, "changelog_uri", out string changelogUri) ? new Uri(changelogUri) : null;
                                SelfUpdateProgressPage.DownloadUris = new List<Uri>(((List<object>)obj["uri"]).Select(uri => new Uri(res.Uri, (string)uri)));
                                SelfUpdateProgressPage.Hash = ((string)obj["hash-sha256"]).FromHexToBin();
                                SelfUpdateProgressPage.Arguments = eduJSON.Parser.GetValue(obj, "arguments", out string installerArguments) ? installerArguments : null;
                            }));
                        },

                        () =>
                        {
                            // Evaluate installed products.
                            Version productVersion = null;
                            var productId = Properties.Settings.Default.SelfUpdateBundleId.ToUpperInvariant();
                            Trace.TraceInformation("Evaluating installed products");
                            using (var hklmKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                            using (var uninstallKey = hklmKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", false))
                            {
                                foreach (var productKeyName in uninstallKey.GetSubKeyNames())
                                {
                                    Abort.Token.ThrowIfCancellationRequested();
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
                            TryInvoke((Action)(() => { InstalledVersion = productVersion; }));
                        },
                    },
                action =>
                {
                    TryInvoke((Action)(() => TaskCount++));
                    try { action(); }
                    finally { TryInvoke((Action)(() => TaskCount--)); }
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
            TryInvoke((Action)(() =>
            {
                if (NavigateTo.CanExecute(SelfUpdatePromptPage))
                {
                    Properties.Settings.Default.SelfUpdateLastVersion = AvailableVersion.ToString();
                    Properties.Settings.Default.SelfUpdateLastReminder = DateTimeOffset.Now;
                    NavigateTo.Execute(SelfUpdatePromptPage);
                }
            }));
        }

        /// <summary>
        /// Ask view to quit.
        /// </summary>
        /// <param name="sender">Event sender</param>
        public void OnQuitApplication(object sender)
        {
            Trace.TraceInformation("Quitting client");
            QuitApplication?.Invoke(sender, EventArgs.Empty);
        }

        /// <summary>
        /// Invoke method on GUI thread if it's not terminating.
        /// </summary>
        /// <param name="method">Method to execute</param>
        /// <returns>The return value from the delegate being invoked or <c>null</c> if the delegate has no return value or dispatcher is shutting down.</returns>
        public object TryInvoke(Delegate method)
        {
            if (Dispatcher.HasShutdownStarted)
                return null;
            return Dispatcher.Invoke(DispatcherPriority.Normal, method);
        }

        #endregion
    }
}

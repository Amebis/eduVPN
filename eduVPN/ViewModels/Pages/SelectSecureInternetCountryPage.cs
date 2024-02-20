/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Secure internet connecting country selection wizard page
    /// </summary>
    public class SelectSecureInternetCountryPage : ConnectWizardStandardPage
    {
        #region Properties

        /// <summary>
        /// Secure internet country list
        /// </summary>
        public ObservableCollectionEx<Country> SecureInternetCountries { get; } = new ObservableCollectionEx<Country>();

        /// <summary>
        /// Selected secure internet country
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Country SelectedSecureInternetCountry
        {
            get => _SelectedSecureInternetCountry;
            set
            {
                if (SetProperty(ref _SelectedSecureInternetCountry, value))
                    _ConfirmSecureInternetCountrySelection?.RaiseCanExecuteChanged();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Country _SelectedSecureInternetCountry;

        /// <summary>
        /// Confirms secure internet country selection
        /// </summary>
        public DelegateCommand ConfirmSecureInternetCountrySelection
        {
            get
            {
                if (_ConfirmSecureInternetCountrySelection == null)
                    _ConfirmSecureInternetCountrySelection = new DelegateCommand(
                        async () =>
                        {
                            var operation = Wizard.OperationInProgress;
                            if (operation != null)
                            {
                                // Country selection was triggered implicitly when adding Secure Internet.
                                await Task.Run(() => operation.Reply(SelectedSecureInternetCountry.Code));
                            }
                            else
                            {
                                // Country selection was triggered explicitly by user on the Home page.
                                Wizard.TaskCount++;
                                try
                                {
                                    var country = SelectedSecureInternetCountry;
                                    Wizard.CurrentPage = Wizard.PleaseWaitPage;
                                    try
                                    {
                                        using (var cookie = new Engine.CancellationTokenCookie(Window.Abort.Token))
                                            await Task.Run(() => Engine.SetSecureInternetLocation(cookie, country.Code));
                                        //await Task.Run(() => Window.Abort.Token.WaitHandle.WaitOne(10000)); // Mock a slow link for testing.

                                        // eduvpn-common does not do callback on country change. Do the bookkeeping manually.
                                        foreach (var srv in Wizard.HomePage.SecureInternetServers)
                                            srv.Country = country;
                                        Wizard.CurrentPage = Wizard.StartingPage;
                                    }
                                    catch { Wizard.CurrentPage = this; throw; }
                                }
                                finally { Wizard.TaskCount--; }
                            }
                        },
                        () => SelectedSecureInternetCountry != null);
                return _ConfirmSecureInternetCountrySelection;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ConfirmSecureInternetCountrySelection;

        /// <inheritdoc/>
        public override DelegateCommand NavigateBack
        {
            get
            {
                if (_NavigateBack == null)
                    _NavigateBack = new DelegateCommand(() =>
                    {
                        Wizard.OperationInProgress?.Cancel();
                        Wizard.CurrentPage = Wizard.HomePage;
                    });
                return _NavigateBack;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _NavigateBack;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a country selection wizard page.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public SelectSecureInternetCountryPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Populates Secure Internet available country list
        /// </summary>
        /// <param name="obj">eduvpn-common provided country list</param>
        /// <param name="country">Country that should be initially selected</param>
        public void SetSecureInternetCountries(IEnumerable<Country> obj, Country country = null)
        {
            var list = SecureInternetCountries.BeginUpdate();
            try
            {
                list.Clear();
                foreach (var c in obj)
                    list.Add(c);
            }
            finally { SecureInternetCountries.EndUpdate(); }
            SelectedSecureInternetCountry = country != null ? SecureInternetCountries.FirstOrDefault(c => c.Equals(country)) : null;
        }

        #endregion
    }
}

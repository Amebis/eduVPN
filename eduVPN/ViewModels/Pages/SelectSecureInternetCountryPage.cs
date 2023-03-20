/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
                        () => Engine.SetSecureInternetLocation(SelectedSecureInternetCountry.Code),
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
                    _NavigateBack = new DelegateCommand(() => Wizard.CurrentPage = Wizard.HomePage);
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
        public void SetSecureInternetCountries(List<object> obj)
        {
            var selected = SelectedSecureInternetCountry?.Code;
            var list = SecureInternetCountries.BeginUpdate();
            try
            {
                list.Clear();
                foreach (var i in obj)
                {
                    if (!(i is string countryCode))
                        continue;
                    list.Add(new Country(countryCode));
                }
            }
            finally { SecureInternetCountries.EndUpdate(); }
            SelectedSecureInternetCountry = selected != null ? SecureInternetCountries.FirstOrDefault(c => c.Code == selected) : null;
        }

        #endregion
    }
}

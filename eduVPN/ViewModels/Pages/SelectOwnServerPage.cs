/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Own server selection wizard page
    /// </summary>
    public class SelectOwnServerPage : ConnectWizardStandardPage
    {
        #region Properties

        /// <summary>
        /// Server address
        /// </summary>
        [CustomValidation(typeof(SelectOwnServerPage), nameof(CheckHostname))]
        public string Hostname
        {
            get => _Hostname;
            set
            {
                if (SetProperty(ref _Hostname, value))
                    _AddServer?.RaiseCanExecuteChanged();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _Hostname = "";

        /// <summary>
        /// Adds own server
        /// </summary>
        public DelegateCommand AddServer
        {
            get
            {
                if (_AddServer == null)
                    _AddServer = new DelegateCommand(
                        () =>
                        {
                            TryParseUri(Hostname, out var uri);
                            try
                            {
                                Wizard.AddAndConnect(new Server(uri.AbsoluteUri));
                                Hostname = "";
                            }
                            catch (OperationCanceledException) { Wizard.CurrentPage = this; }
                        },
                        () => !string.IsNullOrEmpty(Hostname) && !HasErrors);
                return _AddServer;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _AddServer;

        /// <inheritdoc/>
        public override DelegateCommand NavigateBack
        {
            get
            {
                if (_NavigateBack == null)
                    _NavigateBack = new DelegateCommand(
                        () => Wizard.CurrentPage = Wizard.HomePage,
                        () => Wizard.StartingPage != this);
                return _NavigateBack;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _NavigateBack;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a wizard page.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public SelectOwnServerPage(ConnectWizard wizard) :
            base(wizard)
        {
            PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(HasErrors))
                    _AddServer?.RaiseCanExecuteChanged();
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Validates the hostname
        /// </summary>
        /// <param name="input">Hostname</param>
        /// <param name="output">Base URI</param>
        /// <returns><c>true</c> if valid hostname; <c>false</c> otherwise</returns>
        private static bool TryParseUri(string input, out Uri output)
        {
            try
            {
                // Convert hostname to https://hostname.
                output = new UriBuilder("https", input.Trim()).Uri;
                return true;
            }
            catch
            {
                output = null;
                return false;
            }
        }

        /// <summary>
        /// Validates the hostname
        /// </summary>
        /// <param name="value">Hostname</param>
        /// <param name="context">Validation context</param>
        /// <returns><see cref="ValidationResult.Success"/> if valid hostname; <see cref="ValidationResult"/> issue descriptor otherwise</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Must conform to CustomValidationAttribute")]
        public static ValidationResult CheckHostname(string value, ValidationContext context)
        {
            if (!string.IsNullOrEmpty(value) && !TryParseUri(value, out _))
                return new ValidationResult(Resources.Strings.ErrorInvalidHostname);

            return ValidationResult.Success;
        }

        #endregion
    }
}

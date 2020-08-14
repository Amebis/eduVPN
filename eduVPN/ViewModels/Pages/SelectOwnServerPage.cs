/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
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
    public class SelectOwnServerPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Server address
        /// </summary>
        [CustomValidation(typeof(SelectOwnServerPage), nameof(CheckHostname))]
        public string Hostname
        {
            get { return _Hostname; }
            set
            {
                if (SetProperty(ref _Hostname, value))
                    AddServer.RaiseCanExecuteChanged();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _Hostname = "";

        /// <summary>
        /// Adds own server
        /// </summary>
        public DelegateCommand AddServer { get; }

        /// <inheritdoc/>
        public override DelegateCommand NavigateBack { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a wizard page.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public SelectOwnServerPage(ConnectWizard wizard) :
            base(wizard)
        {
            AddServer = new DelegateCommand(
                async () =>
                {
                    try
                    {
                        TryParseUri(Hostname, out var uri);
                        var srv = new Server(uri);
                        srv.RequestAuthorization += Wizard.AuthorizationPage.OnRequestAuthorization;
                        srv.ForgetAuthorization += Wizard.AuthorizationPage.OnForgetAuthorization;
                        await Wizard.AuthorizationPage.TriggerAuthorizationAsync(srv);
                        Wizard.HomePage.AddOwnServer(srv);
                        Wizard.CurrentPage = Wizard.HomePage;
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Wizard.Error = ex; }
                },
                () => !string.IsNullOrEmpty(Hostname) && !HasErrors);

            PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(HasErrors))
                    AddServer.RaiseCanExecuteChanged();
            };

            NavigateBack = new DelegateCommand(
                // execute
                () =>
                {
                    try { Wizard.CurrentPage = Wizard.HomePage; }
                    catch (Exception ex) { Wizard.Error = ex; }
                },

                // canExecute
                () => Wizard.StartingPage != this);
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

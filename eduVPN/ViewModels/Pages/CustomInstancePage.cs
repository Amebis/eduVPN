/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Custom instance source entry wizard page
    /// </summary>
    public class CustomInstancePage : ConnectWizardPage
    {
        #region Properties

        /// <inheritdoc/>
        public override string Title
        {
            get { return Resources.Strings.CustomInstancePageTitle; }
        }

        /// <summary>
        /// Instance host name
        /// </summary>
        [CustomValidation(typeof(CustomInstancePage), nameof(CheckHostname))]
        public string Hostname
        {
            get { return _hostname; }
            set { SetProperty(ref _hostname, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _hostname;

        /// <summary>
        /// Authorize other instance command
        /// </summary>
        public DelegateCommand SelectCustomInstance
        {
            get
            {
                if (_select_custom_instance == null)
                {
                    _select_custom_instance = new DelegateCommand(
                        // execute
                        async () =>
                        {
                            Wizard.ChangeTaskCount(+1);
                            try
                            {
                                TryParseUri(Hostname, out var uri);

                                var instance = Wizard.InstanceSource.ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == uri.AbsoluteUri);
                                if (instance == null)
                                {
                                    instance = new Instance(uri);
                                    instance.RequestAuthorization += Wizard.Instance_RequestAuthorization;
                                    instance.ForgetAuthorization += Wizard.Instance_ForgetAuthorization;

                                    // Trigger initial authorization request.
                                    await Wizard.TriggerAuthorizationAsync(instance);

                                    Wizard.InstanceSource.ConnectingInstanceList.Add(instance);
                                }
                                else
                                {
                                    // Trigger initial authorization request.
                                    await Wizard.TriggerAuthorizationAsync(instance);
                                }

                                // Set authentication/connecting instance.
                                Wizard.InstanceSource.ConnectingInstance = instance;

                                // Go to (instance and) profile selection page.
                                switch (Properties.Settings.Default.ConnectingProfileSelectMode)
                                {
                                    case 0: Wizard.CurrentPage = Wizard.ConnectingProfileSelectPage; break;
                                    case 1: Wizard.CurrentPage = Wizard.RecentConfigurationSelectPage; break;
                                    case 2: Wizard.CurrentPage = Wizard.ConnectingProfileSelectPage; break;
                                    case 3: Wizard.CurrentPage = Wizard.RecentConfigurationSelectPage; break;
                                }
                            }
                            catch (Exception ex) { Wizard.Error = ex; }
                            finally { Wizard.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () =>
                            !String.IsNullOrEmpty(Hostname) &&
                            !HasErrors);

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(Hostname) || e.PropertyName == nameof(HasErrors)) _select_custom_instance.RaiseCanExecuteChanged(); };
                }

                return _select_custom_instance;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _select_custom_instance;

        /// <inheritdoc/>
        public override DelegateCommand NavigateBack
        {
            get
            {
                if (_navigate_back == null)
                {
                    _navigate_back = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Wizard.ChangeTaskCount(+1);
                            try
                            {
                                if (Wizard.HasInstanceSources)
                                    Wizard.CurrentPage = Wizard.InstanceSourceSelectPage;
                                else
                                    Wizard.CurrentPage = Wizard.RecentConfigurationSelectPage;
                            }
                            catch (Exception ex) { Wizard.Error = ex; }
                            finally { Wizard.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () => Wizard.StartingPage != this);

                    // Setup canExecute refreshing.
                    Wizard.PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(Wizard.StartingPage)) _navigate_back.RaiseCanExecuteChanged(); };
                }

                return _navigate_back;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _navigate_back;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public CustomInstancePage(ConnectWizard wizard) :
            base(wizard)
        {
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
        public static ValidationResult CheckHostname(string value, ValidationContext context)
        {
            if (!String.IsNullOrEmpty(value) && !TryParseUri(value, out var output))
                return new ValidationResult(Resources.Strings.ErrorInvalidHostname);

            return ValidationResult.Success;
        }

        #endregion
    }
}

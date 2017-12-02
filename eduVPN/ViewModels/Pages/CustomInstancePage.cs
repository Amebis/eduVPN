﻿/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Custom instance source entry wizard page
    /// </summary>
    public class CustomInstancePage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Instance host name
        /// </summary>
        [CustomValidation(typeof(CustomInstancePage), nameof(CheckHostname))]
        public string Hostname
        {
            get { return _hostname; }
            set { SetProperty(ref _hostname, value); }
        }
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
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                TryParseUri(Hostname, out var uri);

                                var instance = Parent.InstanceSource.ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == uri.AbsoluteUri);
                                if (instance == null)
                                {
                                    instance = new Instance(uri);
                                    instance.RequestAuthorization += Parent.Instance_RequestAuthorization;
                                    instance.ForgetAuthorization += Parent.Instance_ForgetAuthorization;

                                    // Trigger initial authorization request.
                                    await Parent.TriggerAuthorizationAsync(instance);

                                    Parent.InstanceSource.ConnectingInstanceList.Add(instance);
                                }
                                else
                                {
                                    // Trigger initial authorization request.
                                    await Parent.TriggerAuthorizationAsync(instance);
                                }

                                // Set authentication/connecting instance.
                                Parent.InstanceSource.ConnectingInstance = instance;

                                // Go to (instance and) profile selection page.
                                switch (Properties.Settings.Default.ConnectingProfileSelectMode)
                                {
                                    case 0: Parent.CurrentPage = Parent.ConnectingProfileSelectPage; break;
                                    case 1: Parent.CurrentPage = Parent.RecentConfigurationSelectPage; break;
                                    case 2: Parent.CurrentPage = Parent.ConnectingProfileSelectPage; break;
                                    case 3: Parent.CurrentPage = Parent.RecentConfigurationSelectPage; break;
                                }
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () =>
                            !string.IsNullOrEmpty(Hostname) &&
                            !HasErrors);

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(Hostname) || e.PropertyName == nameof(HasErrors)) _select_custom_instance.RaiseCanExecuteChanged(); };
                }

                return _select_custom_instance;
            }
        }
        private DelegateCommand _select_custom_instance;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        public CustomInstancePage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override void DoNavigateBack()
        {
            base.DoNavigateBack();

            Parent.CurrentPage = Parent.InstanceSourceSelectPage;
        }

        /// <inheritdoc/>
        protected override bool CanNavigateBack()
        {
            return true;
        }

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
                output = new UriBuilder("https", input).Uri;
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
        /// <returns><c>ValidationResult.Success</c> if valid hostname; <c>ValidationResult</c> issue descriptor otherwise</returns>
        public static ValidationResult CheckHostname(string value, ValidationContext context)
        {
            if (!string.IsNullOrEmpty(value) && !TryParseUri(value, out var output))
                return new ValidationResult(Resources.Strings.ErrorInvalidHostname);

            return ValidationResult.Success;
        }

        #endregion
    }
}

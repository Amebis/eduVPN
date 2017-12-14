/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOpenVPN.Management;
using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Net;
using System.Windows.Input;

namespace eduVPN.ViewModels.Panels
{
    /// <summary>
    /// 2-Factor authentication response panel base class
    /// </summary>
    public class TwoFactorAuthenticationBasePanel : Panel
    {
        #region Properties

        /// <summary>
        /// Method ID to be used as username
        /// </summary>
        public virtual string ID { get => null; }

        /// <summary>
        /// Method name to display in GUI
        /// </summary>
        public virtual string DisplayName { get => null; }

        /// <summary>
        /// Token generator response
        /// </summary>
        public virtual string Response
        {
            get { return _response; }
            set { SetProperty(ref _response, value); }
        }
        private string _response;

        /// <summary>
        /// Apply response command
        /// </summary>
        public ICommand ApplyResponse
        {
            get
            {
                if (_apply_response == null)
                {
                    _apply_response = new DelegateCommand<UsernamePasswordAuthenticationRequestedEventArgs>(
                        // execute
                        e =>
                        {
                            e.Username = ID;
                            e.Password = (new NetworkCredential("", Response)).SecurePassword;
                        },

                        // canExecute
                        e =>
                            e is UsernamePasswordAuthenticationRequestedEventArgs &&
                            !string.IsNullOrEmpty(Response) &&
                            !HasErrors);

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(Response) || e.PropertyName == nameof(HasErrors)) _apply_response.RaiseCanExecuteChanged(); };
                }

                return _apply_response;
            }
        }
        private DelegateCommand<UsernamePasswordAuthenticationRequestedEventArgs> _apply_response;

        /// <summary>
        /// Apply enrollment command
        /// </summary>
        public ICommand ApplyEnrollment
        {
            get
            {
                if (_apply_enrollment == null)
                {
                    _apply_enrollment = new DelegateCommand<RequestTwoFactorEnrollmentEventArgs>(
                        // execute
                        e =>
                        {
                            e.Credentials = GetEnrollmentCredentials();
                        },

                        // canExecute
                        e =>
                            e is RequestTwoFactorEnrollmentEventArgs &&
                            !string.IsNullOrEmpty(Response) &&
                            !HasErrors);

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(Response) || e.PropertyName == nameof(HasErrors)) _apply_enrollment.RaiseCanExecuteChanged(); };
                }

                return _apply_enrollment;
            }
        }
        private DelegateCommand<RequestTwoFactorEnrollmentEventArgs> _apply_enrollment;

        #endregion

        #region Constructors

        /// <summary>
        /// Construct a panel
        /// </summary>
        /// <param name="parent">The page parent</param>
        public TwoFactorAuthenticationBasePanel(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return DisplayName;
        }

        /// <summary>
        /// Returns 2-Factor Authentication enrollment credentials entered
        /// </summary>
        /// <returns>2-Factor Authentication enrollment credentials</returns>
        protected virtual TwoFactorEnrollmentCredentials GetEnrollmentCredentials()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

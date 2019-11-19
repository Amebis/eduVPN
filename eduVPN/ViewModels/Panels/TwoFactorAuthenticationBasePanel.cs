/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOpenVPN.Management;
using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows.Input;
using System.Windows.Threading;

namespace eduVPN.ViewModels.Panels
{
    /// <summary>
    /// 2-Factor authentication response panel base class
    /// </summary>
    public class TwoFactorAuthenticationBasePanel : Panel
    {
        #region Properties

        /// <summary>
        /// Authenticating instance
        /// </summary>
        public Instance AuthenticatingInstance { get; }

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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _response;

        /// <summary>
        /// Running time since the last response
        /// </summary>
        /// <remarks><c>null</c> when previous response time is unknown</remarks>
        public TimeSpan? LastResponseTime
        {
            get {
                return
                    Properties.Settings.Default.InstanceSettings.TryGetValue(AuthenticatingInstance.Base.AbsoluteUri, out var settings) && settings != null && settings.LastTwoFactorAuthenticationResponse != null ?
                        DateTime.Now - settings.LastTwoFactorAuthenticationResponse :
                        null;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DispatcherTimer _previous_response_time_updater;

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

                            // Update the settings.
                            var key = AuthenticatingInstance.Base.AbsoluteUri;
                            if (!Properties.Settings.Default.InstanceSettings.TryGetValue(key, out var settings))
                                Properties.Settings.Default.InstanceSettings[key] = settings = new Xml.InstanceSettings();
                            settings.LastTwoFactorAuthenticationMethod = ID;
                            settings.LastTwoFactorAuthenticationResponse = DateTime.Now;
                            RaisePropertyChanged(nameof(LastResponseTime));
                        },

                        // canExecute
                        e =>
                            e is UsernamePasswordAuthenticationRequestedEventArgs &&
                            !String.IsNullOrEmpty(Response) &&
                            !HasErrors);

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(Response) || e.PropertyName == nameof(HasErrors)) _apply_response.RaiseCanExecuteChanged(); };
                }

                return _apply_response;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand<UsernamePasswordAuthenticationRequestedEventArgs> _apply_response;

        #endregion

        #region Constructors

        /// <summary>
        /// Construct a panel
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        /// <param name="authenticating_instance">Authenticating instance</param>
        public TwoFactorAuthenticationBasePanel(ConnectWizard wizard, Instance authenticating_instance) :
            base(wizard)
        {
            AuthenticatingInstance = authenticating_instance;

            // Create dispatcher timer.
            _previous_response_time_updater = new DispatcherTimer(
                new TimeSpan(0, 0, 0, 1),
                DispatcherPriority.Normal, (object sender, EventArgs e) => RaisePropertyChanged(nameof(LastResponseTime)),
                Wizard.Dispatcher);
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return DisplayName;
        }

        #endregion
    }
}

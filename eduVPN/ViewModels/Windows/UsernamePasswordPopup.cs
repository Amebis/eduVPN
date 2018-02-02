/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOpenVPN.Management;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace eduVPN.ViewModels.Windows
{
    /// <summary>
    /// Username and password authentication pop-up
    /// </summary>
    public class UsernamePasswordPopup : PasswordPopup
    {
        #region Properties

        /// <summary>
        /// Username
        /// </summary>
        public string Username
        {
            get { return _username; }
            set { SetProperty(ref _username, value); }
        }
        private string _username;

        /// <inheritdoc/>
        public override ICommand ApplyResponse
        {
            get
            {
                if (_apply_response == null)
                {
                    _apply_response = new DelegateCommand<UsernamePasswordAuthenticationRequestedEventArgs>(
                        // execute
                        e =>
                        {
                            base.ApplyResponse.Execute(e);
                            e.Username = Username;

                            // Update settings.
                            var key = Session.AuthenticatingInstance.Base.AbsoluteUri;
                            if (!Properties.Settings.Default.InstanceSettings.TryGetValue(key, out var settings))
                                Properties.Settings.Default.InstanceSettings[key] = settings = new Xml.InstanceSettings();
                            settings.LastUsername = Username;
                        },

                        // canExecute
                        e =>
                            base.ApplyResponse.CanExecute(e) &&
                            e is UsernamePasswordAuthenticationRequestedEventArgs &&
                            Username != null && Username.Length > 0);

                    // Setup canExecute refreshing.
                    base.ApplyResponse.CanExecuteChanged += (object sender, EventArgs e) => _apply_response.RaiseCanExecuteChanged();
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(Username)) _apply_response.RaiseCanExecuteChanged(); };
                }

                return _apply_response;
            }
        }
        private DelegateCommand<UsernamePasswordAuthenticationRequestedEventArgs> _apply_response;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a pop-up window
        /// </summary>
        /// <param name="sender">VPN session</param>
        /// <param name="e">Event arguments</param>
        public UsernamePasswordPopup(object sender, UsernamePasswordAuthenticationRequestedEventArgs e) :
            base(sender, e)
        {
            _username = Properties.Settings.Default.InstanceSettings.TryGetValue(Session.AuthenticatingInstance.Base.AbsoluteUri, out var settings) ? settings.LastUsername : null;
        }

        #endregion
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOpenVPN.Management;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// TOTP authentication response panel class
    /// </summary>
    public class TOTPAuthenticationPanel : TwoFactorAuthenticationBasePanel
    {
        #region Properties

        /// <inheritdoc/>
        public override string ID { get => "totp"; }

        /// <inheritdoc/>
        public override string DisplayName { get => Resources.Strings.TwoFactorAuthenticationMethodTOTP; }

        /// <inheritdoc/>
        public override ICommand ApplyResponse
        {
            get
            {
                if (_apply_response == null)
                {
                    _apply_response = new DelegateCommand<UsernamePasswordAuthenticationRequestedEventArgs>(
                        // execute
                        e => base.ApplyResponse.Execute(e),

                        // canExecute
                        e =>
                            base.ApplyResponse.CanExecute(e) &&
                            Response != null && new Regex(@"^\d{6}$", RegexOptions.Compiled).IsMatch(Response));

                    // Setup canExecute refreshing.
                    base.ApplyResponse.CanExecuteChanged += (object sender, EventArgs e) => _apply_response.RaiseCanExecuteChanged();
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(Response)) _apply_response.RaiseCanExecuteChanged(); };
                }

                return _apply_response;
            }
        }
        private DelegateCommand<UsernamePasswordAuthenticationRequestedEventArgs> _apply_response;

        #endregion
    }
}

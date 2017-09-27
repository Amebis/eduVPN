/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOpenVPN.Management;
using Prism.Commands;
using Prism.Mvvm;
using System.ComponentModel;
using System.Net;
using System.Windows.Input;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// 2-Factor authentication response panel base class
    /// </summary>
    public class TwoFactorAuthenticationBasePanel : BindableBase
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
        public virtual ICommand ApplyResponse
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
                            Response != null && Response.Length > 0);

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(Response)) _apply_response.RaiseCanExecuteChanged(); };
                }

                return _apply_response;
            }
        }
        private DelegateCommand<UsernamePasswordAuthenticationRequestedEventArgs> _apply_response;

        #endregion

        #region Methods

        public override string ToString()
        {
            return DisplayName;
        }

        #endregion
    }
}

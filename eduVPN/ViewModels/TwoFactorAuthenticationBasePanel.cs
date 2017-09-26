/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOpenVPN.Management;
using Prism.Commands;
using Prism.Mvvm;
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
            set
            {
                if (value != _response)
                {
                    _response = value;
                    RaisePropertyChanged();
                    ((DelegateCommandBase)ApplyResponse).RaiseCanExecuteChanged();
                }
            }
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
                            e != null && e is UsernamePasswordAuthenticationRequestedEventArgs &&
                            Response != null && Response.Length > 0);
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

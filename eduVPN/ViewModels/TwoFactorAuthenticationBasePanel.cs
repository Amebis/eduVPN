/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;

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
        public virtual string ID { get; }

        /// <summary>
        /// Token generator response
        /// </summary>
        public virtual string Response
        {
            get { return _response; }
            set { if (value != _response) { _response = value; RaisePropertyChanged(); } }
        }
        private string _response;

        #endregion
    }
}

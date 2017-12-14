/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using System.ComponentModel.DataAnnotations;

namespace eduVPN.ViewModels.Panels
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
        [RegularExpression(@"^\d{6}$", ErrorMessageResourceName = "ErrorInvalidTOTP", ErrorMessageResourceType = typeof(Resources.Strings))]
        public override string Response { get => base.Response; set => base.Response = value; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a panel
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        /// <param name="authenticating_instance">Authenticating instance</param>
        public TOTPAuthenticationPanel(ConnectWizard wizard, Instance authenticating_instance) :
            base(wizard, authenticating_instance)
        {
        }

        #endregion
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

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
    }
}

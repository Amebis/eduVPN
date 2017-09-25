/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.ViewModels
{
    /// <summary>
    /// TOTP authentication response panel class
    /// </summary>
    public class TOTPAuthenticationPanel : TwoFactorAuthenticationBasePanel
    {
        #region Properties

        public override string ID { get { return "totp"; } }

        #endregion

        #region Methods

        public override string ToString()
        {
            return Resources.Strings.TwoFactorAuthenticationMethodTOTP;
        }

        #endregion
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.ViewModels
{
    /// <summary>
    /// YubiKey authentication response panel class
    /// </summary>
    public class YubiKeyAuthenticationPanel : TwoFactorAuthenticationBasePanel
    {
        #region Properties

        public override string ID { get { return "yubi"; } }

        #endregion

        #region Methods

        public override string ToString()
        {
            return Resources.Strings.TwoFactorAuthenticationMethodYubiKey;
        }

        #endregion
    }
}

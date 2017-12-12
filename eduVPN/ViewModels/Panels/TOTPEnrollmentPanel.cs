/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using System.Net;
using System.Security.Cryptography;

namespace eduVPN.ViewModels.Panels
{
    /// <summary>
    /// TOTP authentication response panel class
    /// </summary>
    public class TOTPEnrollmentPanel : TOTPAuthenticationPanel
    {
        #region Fields

        private static readonly string _base32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        #endregion

        #region Properties

        /// <summary>
        /// TOTP secret
        /// </summary>
        public string Secret
        {
            get { return _secret; }
            set { SetProperty(ref _secret, value); }
        }
        private string _secret;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a panel
        /// </summary>
        public TOTPEnrollmentPanel()
        {
            _secret = GenerateSecret();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Randomize TOTP secret
        /// </summary>
        public static string GenerateSecret()
        {
            // Generate random secret.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            var random = new byte[10];
            rng.GetBytes(random);

            // Base32 encode.
            var result = new char[16];
            int offset = 0;
            byte buf = 0, bits = 5;
            foreach (var b in random)
            {
                buf = (byte)((b >> (8 - bits)) | buf);
                result[offset++] = _base32[buf];

                if (bits < 4)
                {
                    buf = (byte)((b >> (3 - bits)) & 0x1f);
                    result[offset++] = _base32[buf];
                    bits += 5;
                }

                bits -= 3;
                buf = (byte)((b << bits) & 0x1f);
            }

            return new string(result);
        }

        /// <inheritdoc/>
        protected override TwoFactorEnrollmentCredentials GetEnrollmentCredentials()
        {
            return new TOTPEnrollmentCredentials()
            {
                Secret = (new NetworkCredential("", Secret)).SecurePassword,
                Response = (new NetworkCredential("", Response)).SecurePassword
            };
        }

        #endregion
    }
}

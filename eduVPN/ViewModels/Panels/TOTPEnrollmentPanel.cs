/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Windows;

namespace eduVPN.ViewModels.Panels
{
    /// <summary>
    /// TOTP authentication response panel class
    /// </summary>
    public class TOTPEnrollmentPanel : TOTPAuthenticationPanel
    {
        #region Fields

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _secret;

        /// <summary>
        /// Copies TOTP secret to the clipboard
        /// </summary>
        public DelegateCommand CopySecret
        {
            get
            {
                if (_copy_secret == null)
                {
                    _copy_secret = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Wizard.ChangeTaskCount(+1);
                            try { Clipboard.SetText(Secret); }
                            finally { Wizard.ChangeTaskCount(-1); }
                        });
                }

                return _copy_secret;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _copy_secret;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a panel
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        /// <param name="authenticating_instance">Authenticating instance</param>
        public TOTPEnrollmentPanel(ConnectWizard wizard, Instance authenticating_instance) :
            base(wizard, authenticating_instance)
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
            var random = new byte[20];
            rng.GetBytes(random);

            // Base32 encode.
            var result = new char[32];
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

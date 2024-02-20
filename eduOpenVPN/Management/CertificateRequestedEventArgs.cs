/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Security.Cryptography.X509Certificates;

namespace eduOpenVPN.Management
{
    /// <summary>
    /// <see cref="Session.CertificateRequested"/> event arguments
    /// </summary>
    public class CertificateRequestedEventArgs : EventArgs
    {
        #region Fields

        /// <summary>
        /// A hint about which certificate is required
        /// </summary>
        public readonly string Hint;

        /// <summary>
        /// Certificate
        /// </summary>
        public X509Certificate2 Certificate;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an event arguments
        /// </summary>
        /// <param name="hint">A hint about which certificate is required</param>
        public CertificateRequestedEventArgs(string hint)
        {
            Hint = hint;
        }

        #endregion
    }
}

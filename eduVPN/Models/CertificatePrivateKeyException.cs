/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.Serialization;

namespace eduVPN.Models
{
    [Serializable]
    public class CertificatePrivateKeyException : ApplicationException, ISerializable
    {
        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        public CertificatePrivateKeyException() :
            this(Resources.Strings.ErrorInvalidPrivateKey)
        {
        }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">Exception message</param>
        public CertificatePrivateKeyException(string message) :
            this(message, null)
        {
        }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception reference</param>
        public CertificatePrivateKeyException(string message, Exception innerException) :
            base(message, innerException)
        {
        }

        #endregion
    }
}

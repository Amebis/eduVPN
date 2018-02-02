/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.Serialization;

namespace eduVPN.Models
{
    /// <summary>
    /// Private key is invalid or of unsupported format.
    /// </summary>
    [Serializable]
    public class CertificatePrivateKeyException : Exception
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

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected CertificatePrivateKeyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}

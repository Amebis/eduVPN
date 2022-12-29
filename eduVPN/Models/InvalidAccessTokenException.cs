/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.Serialization;

namespace eduVPN.Models
{
    /// <summary>
    /// No access token granted.
    /// </summary>
    [Serializable]
    public class InvalidAccessTokenException : Exception
    {
        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public InvalidAccessTokenException(string message) :
            this(message, null)
        {
        }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">Inner exception reference</param>
        public InvalidAccessTokenException(string message, Exception innerException) :
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
        protected InvalidAccessTokenException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}

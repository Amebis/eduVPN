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
    /// No access token granted.
    /// </summary>
    [Serializable]
    class AccessTokenNullException : Exception
    {
        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        public AccessTokenNullException() :
            this(Resources.Strings.ErrorNullAccessToken)
        {
        }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">Exception message</param>
        public AccessTokenNullException(string message) :
            this(message, null)
        {
        }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception reference</param>
        public AccessTokenNullException(string message, Exception innerException) :
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
        protected AccessTokenNullException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}

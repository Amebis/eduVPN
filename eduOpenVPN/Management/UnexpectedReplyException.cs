/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.Serialization;

namespace eduOpenVPN.Management
{
    /// <summary>
    /// The OpenVPN Management reply was not expected.
    /// </summary>
    [Serializable]
    public class UnexpectedReplyException : ProtocolException
    {
        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        public UnexpectedReplyException(string response, int start = 0) :
            this(Resources.Strings.ErrorUnexpectedReply, response, start)
        { }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="response">OpenVPN Management response</param>
        /// <param name="start">Starting offset in <paramref name="response"/></param>
        public UnexpectedReplyException(string message, string response, int start = 0) :
            base(message, response, start)
        {
        }

        #endregion

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected UnexpectedReplyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}

/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace eduOpenVPN.Management
{
    /// <summary>
    /// OpenVPN Management protocol error.
    /// </summary>
    [Serializable]
    public class ProtocolException : Exception
    {
        #region Properties

        /// <summary>
        /// Gets the error message and the response, or only the error message if no response is set.
        /// </summary>
        public override string Message => Response != null ? string.Format(Resources.Strings.ErrorManagementManagementResponse, base.Message, Response) : base.Message;

        /// <summary>
        /// OpenVPN Management response that caused the problem
        /// </summary>
        public string Response { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        public ProtocolException() :
            base()
        { }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ProtocolException(string message) :
            base(message)
        { }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">Inner exception</param>
        public ProtocolException(string message, Exception innerException) :
            base(message, innerException)
        { }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="response">OpenVPN Management response</param>
        /// <param name="start">Starting offset in <paramref name="response"/></param>
        public ProtocolException(string message, string response, int start = 0) :
            base(message)
        {
            Response = response.Length < start + 20 ? response.Substring(start) : response.Substring(start, 19) + "…";
        }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="response">OpenVPN Management response</param>
        /// <param name="start">Starting offset in <paramref name="response"/></param>
        /// <param name="innerException">Inner exception</param>
        public ProtocolException(string message, string response, int start, Exception innerException) :
            base(message, innerException)
        {
            Response = response.Length < start + 20 ? response.Substring(start) : response.Substring(start, 19) + "…";
        }

        #endregion

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected ProtocolException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Response = (string)info.GetValue("Response", typeof(string));
        }

        /// <inheritdoc/>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Response", Response);
        }

        #endregion
    }
}

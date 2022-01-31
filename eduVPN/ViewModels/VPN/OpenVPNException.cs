/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.Serialization;

namespace eduVPN.ViewModels.VPN
{
    /// <summary>
    /// OpenVPN reported fatal error.
    /// </summary>
    [Serializable]
    public class OpenVPNException : Exception
    {
        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        public OpenVPNException()
        {
        }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public OpenVPNException(string message) :
            this(message, null)
        {
        }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">Inner exception reference</param>
        public OpenVPNException(string message, Exception innerException) :
            base(message, innerException)
        {
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.IsNullOrEmpty(Message) ?
                Resources.Strings.OpenVPNStateTypeFatalError :
                Resources.Strings.OpenVPNStateTypeFatalError + "\r\n" + Message;
        }

        #endregion

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected OpenVPNException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}

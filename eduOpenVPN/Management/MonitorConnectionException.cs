/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.Serialization;

namespace eduOpenVPN.Management
{
    /// <summary>
    /// OpenVPN Management connection failed
    /// </summary>
    [Serializable]
    public class MonitorConnectionException : AggregateException
    {
        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        public MonitorConnectionException() :
            this(Resources.Strings.ErrorMonitorConnection, null)
        { }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="innerException">Inner exception</param>
        public MonitorConnectionException(Exception innerException) :
            this(Resources.Strings.ErrorMonitorConnection, innerException)
        { }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">Inner exception</param>
        public MonitorConnectionException(string message, Exception innerException) :
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
        protected MonitorConnectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        #endregion
    }
}

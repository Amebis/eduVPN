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
    /// OpenVPN Management monitor terminated error
    /// </summary>
    [Serializable]
    public class MonitorTerminatedException : AggregateException
    {
        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        public MonitorTerminatedException() :
            this(Resources.Strings.ErrorMonitorTerminated, null)
        { }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="innerException">Inner exception</param>
        public MonitorTerminatedException(Exception innerException) :
            this(Resources.Strings.ErrorMonitorTerminated, innerException)
        { }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">Inner exception</param>
        public MonitorTerminatedException(string message, Exception innerException) :
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
        protected MonitorTerminatedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        #endregion
    }
}

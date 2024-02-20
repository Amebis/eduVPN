/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduOpenVPN.Management
{
    /// <summary>
    /// <see cref="Session.LogReported"/> event arguments
    /// </summary>
    public class LogReportedEventArgs : TimestampedEventArgs
    {
        #region Properties

        /// <summary>
        /// Log message flags
        /// </summary>
        public LogMessageFlags Flags { get; }

        /// <summary>
        /// Log message
        /// </summary>
        public string Message { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an event arguments
        /// </summary>
        /// <param name="timestamp">Timestamp of the log entry</param>
        /// <param name="flags">Log message flags</param>
        /// <param name="message">Log message</param>
        public LogReportedEventArgs(DateTimeOffset timestamp, LogMessageFlags flags, string message) :
            base(timestamp)
        {
            Flags = flags;
            Message = message;
        }

        #endregion
    }
}

/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduOpenVPN.Management
{
    /// <summary>
    /// Timestamped event arguments base class
    /// </summary>
    public class TimestampedEventArgs : EventArgs
    {
        #region Fields

        /// <summary>
        /// Timestamp of the event
        /// </summary>
        public readonly DateTimeOffset TimeStamp;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an event arguments
        /// </summary>
        /// <param name="timestamp">Timestamp of the event</param>
        public TimestampedEventArgs(DateTimeOffset timestamp)
        {
            TimeStamp = timestamp;
        }

        #endregion
    }
}

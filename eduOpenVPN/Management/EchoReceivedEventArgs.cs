/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduOpenVPN.Management
{
    /// <summary>
    /// <see cref="Session.EchoReceived"/> event arguments
    /// </summary>
    public class EchoReceivedEventArgs : TimestampedEventArgs
    {
        #region Properties

        /// <summary>
        /// Echo command
        /// </summary>
        public string Command { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an event arguments
        /// </summary>
        /// <param name="timestamp">Timestamp of the command</param>
        /// <param name="command">Echo command</param>
        public EchoReceivedEventArgs(DateTimeOffset timestamp, string command) :
            base(timestamp)
        {
            Command = command;
        }

        #endregion
    }
}

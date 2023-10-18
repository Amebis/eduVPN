/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduOpenVPN.Management
{
    /// <summary>
    /// Message event arguments base class
    /// </summary>
    public class MessageReportedEventArgs : EventArgs
    {
        #region Fields

        /// <summary>
        /// Descriptive string
        /// </summary>
        public readonly string Message;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an event arguments
        /// </summary>
        /// <param name="message">Descriptive string</param>
        public MessageReportedEventArgs(string message)
        {
            Message = message;
        }

        #endregion
    }
}

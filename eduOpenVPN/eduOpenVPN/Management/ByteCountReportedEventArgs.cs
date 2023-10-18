/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduOpenVPN.Management
{
    /// <summary>
    /// <see cref="Session.ByteCountReported"/> event arguments
    /// </summary>
    public class ByteCountReportedEventArgs : EventArgs
    {
        #region Fields

        /// <summary>
        /// Number of bytes that have been received from the server
        /// </summary>
        public readonly ulong RxBytes;

        /// <summary>
        /// Number of bytes that have been sent to the server
        /// </summary>
        public readonly ulong TxBytes;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an event arguments
        /// </summary>
        /// <param name="bytesIn">Number of bytes that have been received from the server</param>
        /// <param name="bytesOut">Number of bytes that have been sent to the server</param>
        public ByteCountReportedEventArgs(ulong bytesIn, ulong bytesOut)
        {
            RxBytes = bytesIn;
            TxBytes = bytesOut;
        }

        #endregion
    }
}

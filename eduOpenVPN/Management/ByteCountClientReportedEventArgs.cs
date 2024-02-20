/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduOpenVPN.Management
{
    /// <summary>
    /// <see cref="Session.ByteCountClientReported"/> event arguments
    /// </summary>
    public class ByteCountClientReportedEventArgs : ByteCountReportedEventArgs
    {
        #region Properties

        /// <summary>
        /// Client identifier
        /// </summary>
        public uint ClientId { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an event arguments
        /// </summary>
        /// <param name="clientId">Client identifier</param>
        /// <param name="bytesIn">Number of bytes that have been received from the server</param>
        /// <param name="bytesOut">Number of bytes that have been sent to the server</param>
        public ByteCountClientReportedEventArgs(uint clientId, ulong bytesIn, ulong bytesOut) :
            base(bytesIn, bytesOut)
        {
            ClientId = clientId;
        }

        #endregion
    }
}

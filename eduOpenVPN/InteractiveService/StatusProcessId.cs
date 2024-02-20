/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduOpenVPN.InteractiveService
{
    /// <summary>
    /// OpenVPN Interactive Service openvpn.exe process identifier message
    /// </summary>
    public class StatusProcessId : Status
    {
        #region Properties

        /// <summary>
        /// openvpn.exe process identifier
        /// </summary>
        public int ProcessId { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an OpenVPN Interactive Service openvpn.exe process identifier message
        /// </summary>
        /// <param name="pid">openvpn.exe process identifier</param>
        /// <param name="message">Additional error description (optional)</param>
        public StatusProcessId(int pid, string message) :
            base(0, message)
        {
            ProcessId = pid;
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}: 0x{1:X}", Message, ProcessId);
        }

        #endregion
    }
}

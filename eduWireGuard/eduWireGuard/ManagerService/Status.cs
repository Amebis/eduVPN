/*
    eduWireGuard - WireGuard Tunnel Manager Library for eduVPN

    Copyright: 2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.IO;
using System.Text;

namespace eduWireGuard.ManagerService
{
    /// <summary>
    /// WireGuard Tunnel Manager service status message
    /// </summary>
    public class Status
    {
        #region Properties

        /// <summary>
        /// Did operation succeeed?
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Win32 error code
        /// </summary>
        public uint Win32Error { get; private set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an object from stream
        /// </summary>
        /// <param name="reader">Input stream at the location where message_status (without inherited message) is written</param>
        public Status(BinaryReader reader)
        {
            Success = reader.ReadBoolean();
            reader.ReadByte();
            reader.ReadByte();
            reader.ReadByte();
            Win32Error = reader.ReadUInt32();
            Message = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
        }

        #endregion
    }
}

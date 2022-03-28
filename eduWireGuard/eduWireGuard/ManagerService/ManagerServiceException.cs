/*
    eduWireGuard - WireGuard Tunnel Manager Library for eduVPN

    Copyright: 2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace eduWireGuard.ManagerService
{
    /// <summary>
    /// WireGuard Tunnel Manager Service returned an error.
    /// </summary>
    [Serializable]
    public class ManagerServiceException : Exception
    {
        #region Properties

        /// <inheritdoc/>
        public override string Message
        {
            get => string.Format(Resources.Strings.ErrorManagerService, string.Format("0x{0:X}", ErrorNumber), Description);
        }

        /// <summary>
        /// Win32 error number
        /// </summary>
        public uint ErrorNumber { get; }

        /// <summary>
        /// Additional error description
        /// </summary>
        public string Description { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an exception
        /// </summary>
        /// <param name="errorNum">Win32 error number</param>
        /// <param name="description">Additional error description</param>
        public ManagerServiceException(uint errorNum, string description) :
            base()
        {
            ErrorNumber = errorNum;
            Description = description;
        }

        #endregion

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected ManagerServiceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ErrorNumber = (uint)info.GetValue("ErrorNumber", typeof(uint));
            Description = (string)info.GetValue("Description", typeof(string));
        }

        /// <inheritdoc/>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ErrorNumber", ErrorNumber);
            info.AddValue("Description", Description);
        }

        #endregion
    }
}

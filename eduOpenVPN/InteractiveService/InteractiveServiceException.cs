/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace eduOpenVPN.InteractiveService
{
    /// <summary>
    /// OpenVPN Interactive Service returned an error.
    /// </summary>
    [Serializable]
    public class InteractiveServiceException : Exception
    {
        #region Properties

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                var msg = string.Format(Resources.Strings.ErrorInteractiveService, string.Format("0x{0:X}", ErrorNumber), Function);
                return Description != null ? string.Format("{0}\n{1}", msg, Description) : msg;
            }
        }

        /// <summary>
        /// Error number
        /// </summary>
        public uint ErrorNumber { get; }

        /// <summary>
        /// The function that failed
        /// </summary>
        public string Function { get; }

        /// <summary>
        /// Additional error description (optional)
        /// </summary>
        public string Description { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an exception
        /// </summary>
        /// <param name="errorNum">Error number</param>
        /// <param name="function">The function that failed</param>
        /// <param name="errorDescription">Human-readable text providing additional information</param>
        public InteractiveServiceException(uint errorNum, string function, string errorDescription) :
            base()
        {
            ErrorNumber = errorNum;
            Function = function;
            Description = errorDescription;
        }

        #endregion

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected InteractiveServiceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ErrorNumber = (uint)info.GetValue("ErrorNumber", typeof(uint));
            Function = (string)info.GetValue("Function", typeof(string));
            Description = (string)info.GetValue("Description", typeof(string));
        }

        /// <inheritdoc/>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ErrorNumber", ErrorNumber);
            info.AddValue("Function", Function);
            info.AddValue("Description", Description);
        }

        #endregion
    }
}

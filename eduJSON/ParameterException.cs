/*
    eduJSON - Lightweight JSON Parser for eduVPN (and beyond)

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace eduJSON
{
    /// <summary>
    /// Unacceptable or missing parameter.
    /// </summary>
    [Serializable]
    public class ParameterException : Exception
    {
        #region Members

        /// <inheritdoc/>
        public override string Message => ParameterName != null ? string.Format(Resources.Strings.ErrorParameter, base.Message, ParameterName) : base.Message;

        /// <summary>
        /// Parameter name
        /// </summary>
        public string ParameterName { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="parameter">Parameter name</param>
        public ParameterException(string message, string parameter) :
            base(message)
        {
            ParameterName = parameter;
        }

        #endregion

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected ParameterException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ParameterName = (string)info.GetValue("ParameterName", typeof(string));
        }

        /// <inheritdoc/>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ParameterName", ParameterName);
        }

        #endregion
    }
}

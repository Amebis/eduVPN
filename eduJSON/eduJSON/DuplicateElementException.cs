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
    /// ErrorDuplicateElement
    /// </summary>
    [Serializable]
    public class DuplicateElementException : JSONException
    {
        #region Properties

        /// <summary>
        /// Element name
        /// </summary>
        public string ElementName { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="name">Element name</param>
        /// <param name="code">JSON code</param>
        /// <param name="start">Starting offset in <paramref name="code"/></param>
        public DuplicateElementException(string name, string code, int start) :
            this(string.Format(Resources.Strings.ErrorDuplicateElement, name), name, code, start)
        {
        }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="name">Element name</param>
        /// <param name="code">JSON code</param>
        /// <param name="start">Starting offset in <paramref name="code"/></param>
        public DuplicateElementException(string message, string name, string code, int start) :
            base(message, code, start)
        {
            ElementName = name;
        }

        #endregion

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected DuplicateElementException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ElementName = (string)info.GetValue("ElementName", typeof(string));
        }

        /// <inheritdoc/>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ElementName", ElementName);
        }

        #endregion
    }
}

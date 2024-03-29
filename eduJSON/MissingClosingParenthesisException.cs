﻿/*
    eduJSON - Lightweight JSON Parser for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace eduJSON
{
    /// <summary>
    /// Missing "{0}" parenthesis.
    /// </summary>
    [Serializable]
    public class MissingClosingParenthesisException : JSONException
    {
        #region Properties

        /// <summary>
        /// Missing closing parenthesis
        /// </summary>
        public string Parenthesis { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="parenthesis">Parenthesis</param>
        /// <param name="code">JSON code</param>
        /// <param name="start">Starting offset in <paramref name="code"/></param>
        public MissingClosingParenthesisException(string parenthesis, string code, int start) :
            this(string.Format(Resources.Strings.ErrorMissingClosingParenthesis, parenthesis), parenthesis, code, start)
        {
        }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="parenthesis">Parenthesis</param>
        /// <param name="code">JSON code</param>
        /// <param name="start">Starting offset in <paramref name="code"/></param>
        public MissingClosingParenthesisException(string message, string parenthesis, string code, int start) :
            base(message, code, start)
        {
            Parenthesis = parenthesis;
        }

        #endregion

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected MissingClosingParenthesisException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Parenthesis = (string)info.GetValue("Parenthesis", typeof(string));
        }

        /// <inheritdoc/>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Parenthesis", Parenthesis);
        }

        #endregion
    }
}

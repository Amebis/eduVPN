/*
    eduJSON - Lightweight JSON Parser for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.Serialization;

namespace eduJSON
{
    /// <summary>
    /// Missing "," separator or "{0}" parenthesis.
    /// </summary>
    [Serializable]
    public class MissingSeparatorOrClosingParenthesisException : MissingClosingParenthesisException
    {
        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="parenthesis">Parenthesis</param>
        /// <param name="code">JSON code</param>
        /// <param name="start">Starting offset in <paramref name="code"/></param>
        public MissingSeparatorOrClosingParenthesisException(string parenthesis, string code, int start) :
            this(string.Format(Resources.Strings.ErrorMissingSeparatorOrClosingParenthesis, parenthesis), parenthesis, code, start)
        {
        }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="parenthesis">Parenthesis</param>
        /// <param name="code">JSON code</param>
        /// <param name="start">Starting offset in <paramref name="code"/></param>
        public MissingSeparatorOrClosingParenthesisException(string message, string parenthesis, string code, int start) :
            base(message, parenthesis, code, start)
        {
        }

        #endregion

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected MissingSeparatorOrClosingParenthesisException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}

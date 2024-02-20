/*
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
    /// Unexpected parameter type.
    /// </summary>
    [Serializable]
    public class InvalidParameterTypeException : ParameterException
    {
        #region Properties

        /// <inheritdoc/>
        public override string Message => string.Format(Resources.Strings.ErrorExpectedReceived, base.Message, ExpectedType, ProvidedType);

        /// <summary>
        /// The expected type of parameter
        /// </summary>
        public Type ExpectedType { get; }

        /// <summary>
        /// The provided type of parameter
        /// </summary>
        public Type ProvidedType { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="parameter">Parameter name</param>
        /// <param name="expectedType">Expected type</param>
        /// <param name="providedType">Provided type</param>
        public InvalidParameterTypeException(string parameter, Type expectedType, Type providedType) :
            this(Resources.Strings.ErrorInvalidParameterType, parameter, expectedType, providedType)
        {
        }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="parameter">Parameter name</param>
        /// <param name="expectedType">Expected type</param>
        /// <param name="providedType">Provided type</param>
        public InvalidParameterTypeException(string message, string parameter, Type expectedType, Type providedType) :
            base(message, parameter)
        {
            ExpectedType = expectedType;
            ProvidedType = providedType;
        }

        #endregion

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected InvalidParameterTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ExpectedType = (Type)info.GetValue("ExpectedType", typeof(Type));
            ProvidedType = (Type)info.GetValue("ProvidedType", typeof(Type));
        }

        /// <inheritdoc/>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ExpectedType", ExpectedType);
            info.AddValue("ProvidedType", ProvidedType);
        }

        #endregion
    }
}

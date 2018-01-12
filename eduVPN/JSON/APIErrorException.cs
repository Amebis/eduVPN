/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.Serialization;

namespace eduVPN.JSON
{
    /// <summary>
    /// API server replied with an error.
    /// </summary>
    [Serializable]
    public class APIErrorException : Exception
    {
        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        public APIErrorException() :
            this(Resources.Strings.ErrorAPIServerReply)
        {
        }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">Exception message</param>
        public APIErrorException(string message) :
            base(message)
        {
        }

        #endregion

        #region ISerializable Support

        protected APIErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}

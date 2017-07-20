/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace eduVPN.JSON
{
    /// <summary>
    /// API server replied with an error.
    /// </summary>
    [Serializable]
    public class APIErrorException : ApplicationException
    {
        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="code">JSON code</param>
        /// <param name="start">Starting offset in <paramref name="code"/>.</param>
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
    }
}

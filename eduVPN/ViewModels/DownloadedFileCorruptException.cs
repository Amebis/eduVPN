/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.Serialization;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Downloaded file is corrupt.
    /// </summary>
    [Serializable]
    class DownloadedFileCorruptException : ApplicationException, ISerializable
    {
        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        public DownloadedFileCorruptException() :
            this(Resources.Strings.ErrorNullAccessToken)
        {
        }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">Exception message</param>
        public DownloadedFileCorruptException(string message) :
            this(message, null)
        {
        }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception reference</param>
        public DownloadedFileCorruptException(string message, Exception innerException) :
            base(message, innerException)
        {
        }

        #endregion
    }
}

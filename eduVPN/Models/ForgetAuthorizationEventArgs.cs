/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduVPN.Models
{
    /// <summary>
    /// ForgetAuthorization event arguments
    /// </summary>
    public class ForgetAuthorizationEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Requested access token scope
        /// </summary>
        public string Scope { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs event arguments
        /// </summary>
        /// <param name="scope">Requested access token scope</param>
        public ForgetAuthorizationEventArgs(string scope)
        {
            Scope = scope;
        }

        #endregion
    }
}

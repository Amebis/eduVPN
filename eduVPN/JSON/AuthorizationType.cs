/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.ObjectModel;

namespace eduVPN.JSON
{
    /// <summary>
    /// Authorization type
    /// </summary>
    public enum AuthorizationType
    {
        /// <summary>
        /// Access token is specific to each instance and cannot be used by other instances (default).
        /// </summary>
        Local = 0,

        /// <summary>
        /// Access token is issued by a central OAuth server; all instances accept this token.
        /// </summary>
        Federated,

        /// <summary>
        /// Access token from any instance can be used by any other instance.
        /// </summary>
        Distributed
    }
}

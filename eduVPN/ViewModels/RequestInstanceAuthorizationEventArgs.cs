/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.ViewModels
{
    /// <summary>
    /// RequestInstanceAuthorization event arguments
    /// </summary>
    public class RequestInstanceAuthorizationEventArgs : Models.RequestAuthorizationEventArgs
    {
        #region Properties

        /// <summary>
        /// Authenticating instance
        /// </summary>
        public Models.InstanceInfo Instance { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an RequestInstanceAuthorization event arguments
        /// </summary>
        /// <param name="instance"></param>
        public RequestInstanceAuthorizationEventArgs(Models.InstanceInfo instance)
        {
            Instance = instance;
        }

        #endregion
    }
}

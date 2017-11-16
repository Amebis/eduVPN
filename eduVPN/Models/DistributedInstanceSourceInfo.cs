/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN list of instances using distributed authentication
    /// </summary>
    /// <remarks>
    /// Access token from any instance can be used by any other instance.
    /// </remarks>
    public class DistributedInstanceSourceInfo : InstanceSourceInfo
    {
        #region Properties

        /// <inheritdoc/>
        public override Instance AuthenticatingInstance
        {
            get { return _authenticating_instance; }
            set { SetProperty(ref _authenticating_instance, value); }
        }
        private Instance _authenticating_instance;

        #endregion
    }
}

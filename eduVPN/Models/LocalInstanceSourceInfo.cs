/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.ObjectModel;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN list of instances using local authentication
    /// </summary>
    /// <remarks>
    /// Access token is specific to each instance and cannot be used by other instances.
    /// </remarks>
    public class LocalInstanceSourceInfo : InstanceSourceInfo
    {
        #region Properties

        /// <inheritdoc/>
        public override ObservableCollection<InstanceInfo> ConnectingInstanceList
        {
            get { return _connecting_instance_list; }
            set { SetProperty(ref _connecting_instance_list, value); }
        }
        private ObservableCollection<InstanceInfo> _connecting_instance_list = new ObservableCollection<InstanceInfo>();

        #endregion
    }
}

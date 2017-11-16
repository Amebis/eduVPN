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
    public class LocalInstanceSource : InstanceSource
    {
        #region Properties

        /// <inheritdoc/>
        public override ObservableCollection<Instance> ConnectingInstanceList
        {
            get { return _connecting_instance_list; }
            set { SetProperty(ref _connecting_instance_list, value); }
        }
        private ObservableCollection<Instance> _connecting_instance_list = new ObservableCollection<Instance>();

        #endregion
    }
}

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
        public override ObservableCollection<Instance> ConnectingInstanceList { get; } = new ObservableCollection<Instance>();

        /// <summary>
        /// User saved profile list
        /// </summary>
        public ObservableCollection<Profile> ConnectingProfileList { get; } = new ObservableCollection<Profile>();

        #endregion
    }
}

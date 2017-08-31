/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;

namespace eduVPN.Models
{
    /// <summary>
    /// VPN configuration
    /// </summary>
    public class VPNConfiguration : BindableBase
    {
        #region Properties

        /// <summary>
        /// Selected instance source type
        /// </summary>
        public Models.InstanceSourceType InstanceSourceType
        {
            get { return _instance_source_type; }
            set { if (value != _instance_source_type) { _instance_source_type = value; RaisePropertyChanged(); } }
        }
        private Models.InstanceSourceType _instance_source_type;

        /// <summary>
        /// Selected instance source
        /// </summary>
        public Models.InstanceSourceInfo InstanceSource
        {
            get { return _instance_source; }
            set { if (value != _instance_source) { _instance_source = value; RaisePropertyChanged(); } }
        }
        private Models.InstanceSourceInfo _instance_source;

        /// <summary>
        /// Authenticating eduVPN instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.InstanceInfo AuthenticatingInstance
        {
            get { return _authenticating_instance; }
            set { if (value != _authenticating_instance) { _authenticating_instance = value; RaisePropertyChanged(); } }
        }
        private Models.InstanceInfo _authenticating_instance;

        /// <summary>
        /// Connecting eduVPN instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.InstanceInfo ConnectingInstance
        {
            get { return _connecting_instance; }
            set { if (value != _connecting_instance) { _connecting_instance = value; RaisePropertyChanged(); } }
        }
        private Models.InstanceInfo _connecting_instance;

        /// <summary>
        /// Connecting eduVPN instance profile
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.ProfileInfo ConnectingProfile
        {
            get { return _connecting_profile; }
            set { if (value != _connecting_profile) { _connecting_profile = value; RaisePropertyChanged(); } }
        }
        private Models.ProfileInfo _connecting_profile;

        /// <summary>
        /// Popularity factor in the [0.0, 1.0] range (default 0.5)
        /// </summary>
        public float Popularity
        {
            get { return _popularity; }
            set { if (value != _popularity) { _popularity = value; RaisePropertyChanged(); } }
        }
        private float _popularity = 0.5f;

        #endregion

        #region Methods

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as VPNConfiguration;
            if (!InstanceSourceType.Equals(other.InstanceSourceType) ||
                !AuthenticatingInstance.Equals(other.AuthenticatingInstance) ||
                !ConnectingInstance.Equals(other.ConnectingInstance) ||
                !ConnectingProfile.Equals(other.ConnectingProfile))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return
                InstanceSourceType.GetHashCode() ^
                AuthenticatingInstance.GetHashCode() ^
                ConnectingInstance.GetHashCode() ^
                ConnectingProfile.GetHashCode();
        }

        public VPNConfigurationSettings ToSettings()
        {
            if (InstanceSource is Models.LocalInstanceSourceInfo)
            {
                // Local authenticating instance source
                return new Models.LocalVPNConfigurationSettings()
                {
                    Instance = ConnectingInstance,
                    Profile = ConnectingProfile,
                    Popularity = Popularity,
                };
            }
            else if (InstanceSource is Models.DistributedInstanceSourceInfo)
            {
                // Distributed authenticating instance source
                return new Models.DistributedVPNConfigurationSettings()
                {
                    AuthenticatingInstance = AuthenticatingInstance.Base.AbsoluteUri,
                    LastInstance = ConnectingInstance.Base.AbsoluteUri,
                    Popularity = Popularity,
                };
            }
            else if (InstanceSource is Models.FederatedInstanceSourceInfo)
            {
                // Federated authenticating instance source.
                return new Models.FederatedVPNConfigurationSettings()
                {
                    LastInstance = ConnectingInstance.Base.AbsoluteUri,
                    Popularity = Popularity,
                };
            }
            else
                throw new InvalidOperationException();
        }

        #endregion
    }
}

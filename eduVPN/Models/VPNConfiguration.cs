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
    public class VPNConfiguration : BindableBase, ICloneable
    {
        #region Properties

        /// <summary>
        /// Authenticating eduVPN instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.InstanceInfo AuthenticatingInstance
        {
            get { return _authenticating_instance; }
            set { SetProperty(ref _authenticating_instance, value); }
        }
        private Models.InstanceInfo _authenticating_instance;

        /// <summary>
        /// Connecting eduVPN instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.InstanceInfo ConnectingInstance
        {
            get { return _connecting_instance; }
            set { SetProperty(ref _connecting_instance, value); }
        }
        private Models.InstanceInfo _connecting_instance;

        /// <summary>
        /// Connecting eduVPN instance profile
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.ProfileInfo ConnectingProfile
        {
            get { return _connecting_profile; }
            set { SetProperty(ref _connecting_profile, value); }
        }
        private Models.ProfileInfo _connecting_profile;

        /// <summary>
        /// Popularity factor in the [0.0, 1.0] range (default 0.5)
        /// </summary>
        public float Popularity
        {
            get { return _popularity; }
            set { SetProperty(ref _popularity, value); }
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
            if (!AuthenticatingInstance.Equals(other.AuthenticatingInstance) ||
                !ConnectingInstance.Equals(other.ConnectingInstance) ||
                !ConnectingProfile.Equals(other.ConnectingProfile))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return
                AuthenticatingInstance.GetHashCode() ^
                ConnectingInstance.GetHashCode() ^
                ConnectingProfile.GetHashCode();
        }

        /// <summary>
        /// Converts configuration to settings
        /// </summary>
        /// <param name="source_info_type">The type of settings required. Must be one of <c></c></param>
        /// <returns></returns>
        public VPNConfigurationSettings ToSettings(Type source_info_type)
        {
            if (source_info_type == typeof(Models.LocalInstanceSourceInfo))
            {
                // Local authenticating instance source
                return new Models.LocalVPNConfigurationSettings()
                {
                    Instance = ConnectingInstance,
                    Profile = ConnectingProfile,
                    Popularity = Popularity,
                };
            }
            else if (source_info_type == typeof(Models.DistributedInstanceSourceInfo))
            {
                // Distributed authenticating instance source
                return new Models.DistributedVPNConfigurationSettings()
                {
                    AuthenticatingInstance = AuthenticatingInstance.Base.AbsoluteUri,
                    LastInstance = ConnectingInstance.Base.AbsoluteUri,
                    Popularity = Popularity,
                };
            }
            else if (source_info_type == typeof(Models.FederatedInstanceSourceInfo))
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

        #region IClonable Support

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion
    }
}

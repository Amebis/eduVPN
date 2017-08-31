/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
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
        /// Authenticating eduVPN instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.InstanceInfo AuthenticatingInstance
        {
            get { return _authenticating_instance; }
            set { _authenticating_instance = value; RaisePropertyChanged(); }
        }
        private Models.InstanceInfo _authenticating_instance;

        /// <summary>
        /// Connecting eduVPN instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.InstanceInfo ConnectingInstance
        {
            get { return _connecting_instance; }
            set { _connecting_instance = value; RaisePropertyChanged(); }
        }
        private Models.InstanceInfo _connecting_instance;

        /// <summary>
        /// Connecting eduVPN instance profile
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.ProfileInfo ConnectingProfile
        {
            get { return _connecting_profile; }
            set { _connecting_profile = value; RaisePropertyChanged(); }
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
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as VPNConfiguration;
            if (!AuthenticatingInstance.Equals(other.AuthenticatingInstance) ||
                !ConnectingInstance.Equals(other.ConnectingInstance) ||
                !ConnectingProfile.Equals(other.ConnectingProfile))
                return false;

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return
                base.GetHashCode() ^
                AuthenticatingInstance.GetHashCode() ^
                ConnectingInstance.GetHashCode() ^
                ConnectingProfile.GetHashCode();
        }

        #endregion
    }
}

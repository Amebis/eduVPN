/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;
using System;
using System.Collections.ObjectModel;
using System.Linq;

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

        #region Methods

        /// <inheritdoc/>
        public override void FromSettings(ConnectWizard parent, Xml.InstanceSourceSettingsBase settings)
        {
            if (settings is Xml.LocalInstanceSourceSettings h_local)
            {
                // - Restore instance list.
                // - Restore connecting instance (optional).
                foreach (var h_instance in h_local.ConnectingInstanceList)
                {
                    var connecting_instance = InstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_instance.Base.AbsoluteUri);
                    if (connecting_instance == null)
                    {
                        // The connecting instance was not found. Could be user entered, or removed from discovery file.
                        connecting_instance = new Instance(h_instance.Base);
                        connecting_instance.RequestAuthorization += parent.Instance_RequestAuthorization;
                        connecting_instance.ForgetAuthorization += parent.Instance_ForgetAuthorization;
                    }
                    else
                        connecting_instance.Popularity = h_instance.Popularity;

                    var instance = ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == connecting_instance.Base.AbsoluteUri);
                    if (instance == null)
                    {
                        ConnectingInstanceList.Add(connecting_instance);
                        instance = connecting_instance;
                    }
                    else
                        instance.Popularity = Math.Max(instance.Popularity, h_instance.Popularity);

                    // Restore connecting profiles (optionally).
                    // Matching profile with existing profiles might trigger OAuth in GetProfileList().
                    switch (Properties.Settings.Default.ConnectingProfileSelectMode)
                    {
                        case 0:
                            {
                                // Restore only profiles user connected to before.
                                var profile_list = instance.GetProfileList(instance, Window.Abort.Token);
                                foreach (var h_profile in h_instance.ProfileList)
                                {
                                    var profile = profile_list.FirstOrDefault(prof => prof.ID == h_profile.ID);
                                    if (profile != null)
                                    {
                                        profile.Popularity = h_profile.Popularity;
                                        if (ConnectingProfileList.FirstOrDefault(prof => prof.Equals(profile)) == null)
                                            ConnectingProfileList.Add(profile);
                                    }
                                }
                            }

                            break;

                        case 2:
                            {
                                // Add all available profiles to the connecting profile list.
                                // Restore popularity on the fly (or leave default to promote newly discovered profiles).
                                var profile_list = instance.GetProfileList(instance, Window.Abort.Token);
                                foreach (var profile in profile_list)
                                {
                                    var h_profile = h_instance.ProfileList.FirstOrDefault(prof => prof.ID == profile.ID);
                                    if (h_profile != null)
                                        profile.Popularity = h_profile.Popularity;

                                    ConnectingProfileList.Add(profile);
                                }
                            }

                            break;
                    }
                }
                ConnectingInstance = h_local.ConnectingInstance != null ? ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_local.ConnectingInstance.AbsoluteUri) : null;
            }
        }

        /// <inheritdoc/>
        public override Xml.InstanceSourceSettingsBase ToSettings()
        {
            return
                new Xml.LocalInstanceSourceSettings()
                {
                    ConnectingInstance = ConnectingInstance?.Base,
                    ConnectingInstanceList = new Xml.InstanceRefList(
                        ConnectingInstanceList
                        .Select(inst =>
                            new Xml.InstanceRef()
                            {
                                Base = inst.Base,
                                Popularity = inst.Popularity,
                                ProfileList = new Xml.ProfileRefList(
                                    ConnectingProfileList
                                    .Where(prof => prof.Instance.Equals(inst))
                                    .Select(prof => new Xml.ProfileRef()
                                    {
                                        ID = prof.ID,
                                        Popularity = prof.Popularity
                                    }))
                            }
                        ))
                };
        }

        #endregion
    }
}

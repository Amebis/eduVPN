/*
    eduVPN - VPN for education and research

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
        public override void FromSettings(ConnectWizard wizard, Xml.InstanceSourceSettingsBase settings)
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
                        connecting_instance.RequestAuthorization += wizard.Instance_RequestAuthorization;
                        connecting_instance.ForgetAuthorization += wizard.Instance_ForgetAuthorization;
                    }
                    connecting_instance.Popularity = h_instance.Popularity;

                    // Restore connecting profiles (optionally).
                    ObservableCollection<Profile> profile_list = null;
                    try
                    {
                        switch (Properties.Settings.Default.ConnectingProfileSelectMode)
                        {
                            case 0:
                            case 2:
                                // This might trigger OAuth.
                                profile_list = connecting_instance.GetProfileList(connecting_instance, Window.Abort.Token);
                                break;
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch
                    {
                        // When profile list could not be obtained from the instance, instance settings should be forgotten to avoid issues next time.
                        connecting_instance.Forget();
                        continue;
                    }
                    switch (Properties.Settings.Default.ConnectingProfileSelectMode)
                    {
                        case 0:
                            {
                                // Restore only profiles user connected to before.
                                foreach (var h_profile in h_instance.ProfileList)
                                {
                                    var profile = profile_list.FirstOrDefault(prof => prof.ID == h_profile.ID);
                                    if (profile != null)
                                    {
                                        // Synchronise profile data.
                                        h_profile.DisplayName = profile.DisplayName;
                                        profile.Popularity = h_profile.Popularity;
                                    }
                                    else
                                    {
                                        // The profile is gone missing. Create an unavailable profile placeholder.
                                        profile = new Profile
                                        {
                                            Instance    = connecting_instance,
                                            ID          = h_profile.ID,
                                            DisplayName = h_profile.DisplayName,
                                            Popularity  = h_profile.Popularity
                                        };
                                        profile.RequestAuthorization += (object sender_profile, RequestAuthorizationEventArgs e_profile) => connecting_instance.OnRequestAuthorization(connecting_instance, e_profile);
                                    }

                                    // Add to the list of connecting profiles.
                                    if (ConnectingProfileList.FirstOrDefault(prof => prof.Equals(profile)) == null)
                                        ConnectingProfileList.Add(profile);
                                }
                            }
                            break;

                        case 2:
                            {
                                // Add all available profiles to the connecting profile list.
                                // Restore popularity on the fly (or leave default to promote newly discovered profiles).
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

                    var instance = ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == connecting_instance.Base.AbsoluteUri);
                    if (instance == null)
                        ConnectingInstanceList.Add(connecting_instance);
                }
                ConnectingInstance = SelectConnectingInstance(h_local.ConnectingInstance);
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
                                        DisplayName = prof.DisplayName,
                                        Popularity = prof.Popularity
                                    }))
                            }
                        ))
                };
        }

        /// <summary>
        /// Removes given instance from history
        /// </summary>
        /// <param name="instance">Instance</param>
        public override void ForgetInstance(Instance instance)
        {
            // Remove all instance profiles from history.
            for (var i = ConnectingProfileList.Count; i-- > 0;)
                if (ConnectingProfileList[i].Instance.Equals(instance))
                    ConnectingProfileList.RemoveAt(i);

            if (ConnectingInstance != null && ConnectingInstance.Equals(instance))
            {
                base.ForgetInstance(instance);

                // Reset authenticating instance.
                AuthenticatingInstance = ConnectingInstance;
            }
            else
                base.ForgetInstance(instance);
        }

        /// <summary>
        /// Removes entire instance source history
        /// </summary>
        public override void Forget()
        {
            // Remove all profiles from history.
            ConnectingProfileList.Clear();

            base.Forget();
        }

        #endregion
    }
}

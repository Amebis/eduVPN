/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using System;
using System.Linq;

namespace eduVPN
{
    public class Global
    {
        #region Fields

        /// <summary>
        /// Instance directory URI IDs as used in <c>Properties.Settings.Default</c> collection
        /// </summary>
        public static readonly string[] InstanceDirectoryId = new string[]
        {
            null,
            "SecureInternet",
            "InstituteAccess",
        };

        #endregion

        #region Methods

        /// <summary>
        /// View model global initialization
        /// </summary>
        public static void Initialize()
        {
            if (Properties.Settings.Default.SettingsVersion == 0)
            {
                // Migrate settings from previous version.
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.SettingsVersion = 1;

                // Versions before 1.0.4 used interface name, instead of ID.
                if (Properties.Settings.Default.GetPreviousVersion("OpenVPNInterface") is string iface_name &&
                    NetworkInterface.TryFromName(iface_name, out var iface))
                    Properties.Settings.Default.OpenVPNInterfaceID = iface.ID;

                for (int source_index = (int)InstanceSourceType._start; source_index < (int)InstanceSourceType._end; source_index++)
                {
                    #pragma warning disable 0612 // This section contains legacy settings conversion.
                    if (Properties.Settings.Default.GetPreviousVersion(InstanceDirectoryId[source_index] + "ConfigHistory") is Xml.VPNConfigurationSettingsList settings_list &&
                        settings_list.Count > 0)
                    {
                        // Versions before 1.0.9 used different instance source settings. Convert them.
                        Xml.InstanceSourceSettingsBase h = null;
                        if (settings_list[0] is Xml.LocalVPNConfigurationSettings)
                        {
                            // Local authenticating instance source:
                            // - Convert instance list.
                            // - Set connecting instance by maximum popularity.
                            var h_local = new Xml.LocalInstanceSourceSettings();
                            foreach (var h_cfg in settings_list)
                            {
                                if (h_cfg is Xml.LocalVPNConfigurationSettings h_cfg_local)
                                {
                                    var instance = h_local.ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_cfg_local.Instance.AbsoluteUri);
                                    if (instance == null)
                                    {
                                        // Add (instance, profile) pair.
                                        h_local.ConnectingInstanceList.Add(new Xml.InstanceRef()
                                        {
                                            Base = h_cfg_local.Instance,
                                            Popularity = h_cfg_local.Popularity,
                                            ProfileList = new Xml.ProfileRefList()
                                            {
                                                new Xml.ProfileRef()
                                                {
                                                    ID = h_cfg_local.Profile,
                                                    Popularity = h_cfg_local.Popularity
                                                }
                                            }
                                        });
                                    }
                                    else
                                    {
                                        // Instance already on the list. Update it.
                                        instance.Popularity = Math.Max(instance.Popularity, h_cfg_local.Popularity);
                                        if (instance.ProfileList.FirstOrDefault(prof => prof.ID == h_cfg_local.Profile) == null)
                                        {
                                            // Add profile to the instance.
                                            instance.ProfileList.Add(new Xml.ProfileRef()
                                            {
                                                ID = h_cfg_local.Profile,
                                                Popularity = h_cfg_local.Popularity
                                            });
                                        }
                                    }
                                }
                            }
                            h_local.ConnectingInstance = h_local.ConnectingInstanceList.Count > 0 ? h_local.ConnectingInstanceList.Aggregate((most_popular_instance, inst) => (most_popular_instance == null || inst.Popularity > most_popular_instance.Popularity ? inst : most_popular_instance))?.Base : null;
                            h = h_local;
                        }
                        else if (settings_list[0] is Xml.DistributedVPNConfigurationSettings h_cfg_distributed)
                        {
                            // Distributed authenticating instance source:
                            // - Convert authenticating instance.
                            // - Convert connecting instance.
                            h = new Xml.DistributedInstanceSourceSettings
                            {
                                AuthenticatingInstance = new Uri(h_cfg_distributed.AuthenticatingInstance),
                                ConnectingInstance = new Uri(h_cfg_distributed.LastInstance)
                            };
                        }
                        else if (settings_list[0] is Xml.FederatedVPNConfigurationSettings h_cfg_federated)
                        {
                            // Federated authenticating instance source:
                            // - Convert connecting instance.
                            h = new Xml.FederatedInstanceSourceSettings
                            {
                                ConnectingInstance = new Uri(h_cfg_federated.LastInstance)
                            };
                        }

                        Properties.Settings.Default[InstanceDirectoryId[source_index] + "InstanceSourceSettings"] = new Xml.InstanceSourceSettings() { InstanceSource = h };
                    }
                    #pragma warning restore 0612
                }
            }
        }

        #endregion
    }
}

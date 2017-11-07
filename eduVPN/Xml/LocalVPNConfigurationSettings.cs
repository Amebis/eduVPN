/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Xml;

namespace eduVPN.Xml
{
    /// <summary>
    /// VPN configuration for local authenticating instance source as persisted to settings
    /// </summary>
    [Obsolete]
    public class LocalVPNConfigurationSettings : VPNConfigurationSettings
    {
        #region Properties

        /// <summary>
        /// Instance
        /// </summary>
        public Models.InstanceInfo Instance { get; set; }

        /// <summary>
        /// Profile ID
        /// </summary>
        public Models.ProfileInfo Profile { get; set; }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as LocalVPNConfigurationSettings;
            if (!Instance.Base.Equals(other.Instance.Base) ||
                !Profile.ID.Equals(other.Profile.ID))
                return false;

            return base.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode() ^ Instance.Base.GetHashCode() ^ Profile.ID.GetHashCode();
        }

        #endregion

        #region IXmlSerializable Support

        /// <inheritdoc/>
        public override void ReadXml(XmlReader reader)
        {
            Instance = null;
            Profile = null;

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case nameof(VPNConfigurationSettings):
                            base.ReadXml(reader);
                            break;

                        case nameof(Models.InstanceInfo):
                            if (reader["Key"] == nameof(Instance))
                            {
                                Instance = new Models.InstanceInfo();
                                Instance.ReadXml(reader);
                            }
                            break;

                        case nameof(Models.ProfileInfo):
                            if (reader["Key"] == nameof(Profile))
                            {
                                Profile = new Models.ProfileInfo();
                                Profile.ReadXml(reader);
                            }
                            break;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(nameof(VPNConfigurationSettings));
            base.WriteXml(writer);
            writer.WriteEndElement();

            if (Instance != null)
            {
                writer.WriteStartElement(nameof(Models.InstanceInfo));
                writer.WriteAttributeString("Key", nameof(Instance));
                Instance.WriteXml(writer);
                writer.WriteEndElement();
            }

            if (Profile != null)
            {
                writer.WriteStartElement(nameof(Models.ProfileInfo));
                writer.WriteAttributeString("Key", nameof(Profile));
                Profile.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}

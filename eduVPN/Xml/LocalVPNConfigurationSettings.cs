/*
    eduVPN - VPN for education and research

    Copyright: 2017-2018 The Commons Conservancy eduVPN Programme
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
        /// Instance base URI
        /// </summary>
        public Uri Instance { get; set; }

        /// <summary>
        /// Profile ID
        /// </summary>
        public string Profile { get; set; }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as LocalVPNConfigurationSettings;
            if (!Instance.AbsoluteUri.Equals(other.Instance.AbsoluteUri) ||
                !Profile.Equals(other.Profile))
                return false;

            return base.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode() ^ Instance.AbsoluteUri.GetHashCode() ^ Profile.GetHashCode();
        }

        #endregion

        #region IXmlSerializable Support

        /// <inheritdoc/>
        public override void ReadXml(XmlReader reader)
        {
            string v;

            if (reader.IsEmptyElement)
                return;

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

                        case "InstanceInfo":
                            if (reader["Key"] == nameof(Instance))
                                Instance = (v = reader["Base"]) != null ? new Uri(v) : null;
                            break;

                        case "ProfileInfo":
                            if (reader["Key"] == nameof(Profile))
                                Profile = reader["ID"];
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
                writer.WriteStartElement("InstanceInfo");
                writer.WriteAttributeString("Key", nameof(Instance));
                writer.WriteAttributeString("Base", Instance.AbsoluteUri);
                writer.WriteEndElement();
            }

            if (Profile != null)
            {
                writer.WriteStartElement("ProfileInfo");
                writer.WriteAttributeString("Key", nameof(Profile));
                writer.WriteAttributeString("ID", Profile);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Xml;

namespace eduVPN.Models
{
    /// <summary>
    /// VPN configuration for local authenticating instance source as persisted to settings
    /// </summary>
    public class LocalVPNConfigurationSettings : VPNConfigurationSettings
    {
        #region Properties

        /// <summary>
        /// Instance
        /// </summary>
        public InstanceInfo Instance { get; set; }

        /// <summary>
        /// Profile ID
        /// </summary>
        public string Profile { get; set; }

        #endregion

        #region Methods

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as LocalVPNConfigurationSettings;
            if (!Instance.Base.Equals(other.Instance.Base) ||
                !Profile.Equals(other.Profile))
                return false;

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ Instance.Base.GetHashCode() ^ Profile.GetHashCode();
        }

        #endregion

        #region IXmlSerializable Support

        public override void ReadXml(XmlReader reader)
        {
            Profile = reader.GetAttribute("Profile");

            Instance = null;

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "LocalVPNConfigurationSettings"))
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "VPNConfigurationSettings":
                            base.ReadXml(reader);
                            break;

                        case "InstanceInfo":
                            if (reader["Key"] == "Instance")
                            {
                                Instance = new InstanceInfo();
                                Instance.ReadXml(reader);
                            }
                            break;
                    }
                }
            }
        }

        public override void WriteXml(XmlWriter writer)
        {
            if (Profile != null)
                writer.WriteAttributeString("Profile", Profile);

            writer.WriteStartElement("VPNConfigurationSettings");
            base.WriteXml(writer);
            writer.WriteEndElement();

            if (Instance != null)
            {
                writer.WriteStartElement("InstanceInfo");
                writer.WriteAttributeString("Key", "Instance");
                Instance.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}

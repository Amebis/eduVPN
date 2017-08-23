/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using System.Xml;

namespace eduVPN.Models
{
    /// <summary>
    /// VPN configuration for local authenticating instance group as persisted to settings
    /// </summary>
    public class LocalVPNConfigurationSettings : VPNConfigurationSettings
    {
        #region Properties

        /// <summary>
        /// Instance ID
        /// </summary>
        public string Instance { get; set; }

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
            if (!Instance.Equals(other.Instance) ||
                !Profile.Equals(other.Profile))
                return false;

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ Instance.GetHashCode() ^ Profile.GetHashCode();
        }

        #endregion

        #region IXmlSerializable Support

        public override void ReadXml(XmlReader reader)
        {
            Instance = null;
            Profile = null;

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "LocalVPNConfigurationSettings"))
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "VPNConfigurationSettings": base.ReadXml(reader); break;
                        case "Instance": Instance = reader["Key"]; break;
                        case "Profile": Profile = reader["Key"]; break;
                    }
                }
            }
        }

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("VPNConfigurationSettings");
            base.WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteStartElement("Instance");
            writer.WriteAttributeString("Key", Instance);
            writer.WriteEndElement();

            writer.WriteStartElement("Profile");
            writer.WriteAttributeString("Key", Profile);
            writer.WriteEndElement();
        }

        #endregion
    }
}

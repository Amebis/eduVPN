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
    /// VPN configuration for federated authenticating instance group as persisted to settings
    /// </summary>
    public class FederatedVPNConfigurationSettings : VPNConfigurationSettings
    {
        #region Properties

        /// <summary>
        /// Last connected instance ID
        /// </summary>
        public string LastInstance { get; set; }

        #endregion

        #region Methods

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as FederatedVPNConfigurationSettings;
            if (!LastInstance.Equals(other.LastInstance))
                return false;

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ LastInstance.GetHashCode();
        }

        #endregion

        #region IXmlSerializable Support

        public override void ReadXml(XmlReader reader)
        {
            LastInstance = null;

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "FederatedVPNConfigurationSettings"))
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "VPNConfigurationSettings": base.ReadXml(reader); break;
                        case "LastInstance": LastInstance = reader["Key"]; break;
                    }
                }
            }
        }

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("VPNConfigurationSettings");
            base.WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteStartElement("LastInstance");
            writer.WriteAttributeString("Key", LastInstance);
            writer.WriteEndElement();
        }

        #endregion
    }
}

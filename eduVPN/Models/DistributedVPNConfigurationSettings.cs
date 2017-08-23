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
    /// VPN configuration for distributed authenticating instance group as persisted to settings
    /// </summary>
    public class DistributedVPNConfigurationSettings : FederatedVPNConfigurationSettings
    {
        #region Properties

        /// <summary>
        /// Authenticating instance ID
        /// </summary>
        public string AuthenticatingInstance { get; set; }

        #endregion

        #region Methods

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as DistributedVPNConfigurationSettings;
            if (!AuthenticatingInstance.Equals(other.AuthenticatingInstance))
                return false;

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ AuthenticatingInstance.GetHashCode();
        }

        #endregion
        
        #region IXmlSerializable Support

        public override void ReadXml(XmlReader reader)
        {
            AuthenticatingInstance = null;

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "DistributedVPNConfigurationSettings"))
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "FederatedVPNConfigurationSettings": base.ReadXml(reader); break;
                        case "AuthenticatingInstance": AuthenticatingInstance = reader["Key"]; break;
                    }
                }
            }
        }

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("FederatedVPNConfigurationSettings");
            base.WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteStartElement("AuthenticatingInstance");
            writer.WriteAttributeString("Key", AuthenticatingInstance);
            writer.WriteEndElement();
        }

        #endregion
    }
}

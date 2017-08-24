/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

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
        /// Authenticating instance base URI
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
            AuthenticatingInstance = reader["AuthenticatingInstance"];

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "DistributedVPNConfigurationSettings"))
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "FederatedVPNConfigurationSettings")
                    base.ReadXml(reader);
            }
        }

        public override void WriteXml(XmlWriter writer)
        {
            if (AuthenticatingInstance != null)
                writer.WriteAttributeString("AuthenticatingInstance", AuthenticatingInstance);

            writer.WriteStartElement("FederatedVPNConfigurationSettings");
            base.WriteXml(writer);
            writer.WriteEndElement();
        }

        #endregion
    }
}

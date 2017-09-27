/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Xml;

namespace eduVPN.Models
{
    /// <summary>
    /// VPN configuration for federated authenticating instance source as persisted to settings
    /// </summary>
    public class FederatedVPNConfigurationSettings : VPNConfigurationSettings
    {
        #region Properties

        /// <summary>
        /// Last connected instance base URI
        /// </summary>
        public string LastInstance { get; set; }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as FederatedVPNConfigurationSettings;
            if (!LastInstance.Equals(other.LastInstance))
                return false;

            return base.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode() ^ LastInstance.GetHashCode();
        }

        #endregion

        #region IXmlSerializable Support

        /// <inheritdoc/>
        public override void ReadXml(XmlReader reader)
        {
            LastInstance = reader["LastInstance"];

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "FederatedVPNConfigurationSettings"))
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "VPNConfigurationSettings")
                    base.ReadXml(reader);
            }
        }

        /// <inheritdoc/>
        public override void WriteXml(XmlWriter writer)
        {
            if (LastInstance != null)
                writer.WriteAttributeString("LastInstance", LastInstance);

            writer.WriteStartElement("VPNConfigurationSettings");
            base.WriteXml(writer);
            writer.WriteEndElement();
        }

        #endregion
    }
}

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
    /// VPN configuration for distributed authenticating instance source as persisted to settings
    /// </summary>
    [Obsolete]
    public class DistributedVPNConfigurationSettings : FederatedVPNConfigurationSettings
    {
        #region Properties

        /// <summary>
        /// Authenticating instance base URI
        /// </summary>
        public string AuthenticatingInstance { get; set; }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as DistributedVPNConfigurationSettings;
            if (!AuthenticatingInstance.Equals(other.AuthenticatingInstance))
                return false;

            return base.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode() ^ AuthenticatingInstance.GetHashCode();
        }

        #endregion

        #region IXmlSerializable Support

        /// <inheritdoc/>
        public override void ReadXml(XmlReader reader)
        {
            AuthenticatingInstance = reader[nameof(AuthenticatingInstance)];

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == nameof(FederatedVPNConfigurationSettings))
                    base.ReadXml(reader);
            }
        }

        /// <inheritdoc/>
        public override void WriteXml(XmlWriter writer)
        {
            if (AuthenticatingInstance != null)
                writer.WriteAttributeString(nameof(AuthenticatingInstance), AuthenticatingInstance);

            writer.WriteStartElement(nameof(FederatedVPNConfigurationSettings));
            base.WriteXml(writer);
            writer.WriteEndElement();
        }

        #endregion
    }
}

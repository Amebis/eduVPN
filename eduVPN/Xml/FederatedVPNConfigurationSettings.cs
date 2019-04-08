/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Xml;

namespace eduVPN.Xml
{
    /// <summary>
    /// VPN configuration for federated authenticating instance source as persisted to settings
    /// </summary>
    [Obsolete]
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
            LastInstance = reader[nameof(LastInstance)];

            if (reader.IsEmptyElement)
                return;

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == nameof(VPNConfigurationSettings))
                    base.ReadXml(reader);
            }
        }

        /// <inheritdoc/>
        public override void WriteXml(XmlWriter writer)
        {
            if (LastInstance != null)
                writer.WriteAttributeString(nameof(LastInstance), LastInstance);

            writer.WriteStartElement(nameof(VPNConfigurationSettings));
            base.WriteXml(writer);
            writer.WriteEndElement();
        }

        #endregion
    }
}

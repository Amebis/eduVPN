/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Xml;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable instance source using federated authentication
    /// </summary>
    /// <remarks>
    /// Access token is issued by a central OAuth server; all instances accept this token.
    /// </remarks>
    public class FederatedInstanceSourceSettings : InstanceSourceSettingsBase
    {
        #region IXmlSerializable Support

        /// <inheritdoc/>
        public override void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement)
                return;

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case nameof(InstanceSourceSettingsBase):
                            base.ReadXml(reader);
                            break;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(nameof(InstanceSourceSettingsBase));
            base.WriteXml(writer);
            writer.WriteEndElement();
        }

        #endregion
    }
}

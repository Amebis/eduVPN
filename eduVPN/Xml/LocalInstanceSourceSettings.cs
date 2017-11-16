/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Xml;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable instance source using local authentication
    /// </summary>
    /// <remarks>
    /// Access token is specific to each instance and cannot be used by other instances.
    /// </remarks>
    public class LocalInstanceSourceSettings : InstanceSourceSettingsBase
    {
        #region Properties

        /// <summary>
        /// Connecting instance list
        /// </summary>
        public InstanceRefList ConnectingInstanceList { get; set; } = new InstanceRefList();

        #endregion

        #region IXmlSerializable Support

        /// <inheritdoc/>
        public override void ReadXml(XmlReader reader)
        {
            ConnectingInstanceList.Clear();

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case nameof(InstanceRefList):
                            if (reader["Key"] == nameof(ConnectingInstanceList))
                                if (!reader.IsEmptyElement)
                                    ConnectingInstanceList.ReadXml(reader);
                            break;

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
            writer.WriteStartElement(nameof(InstanceRefList));
            writer.WriteAttributeString("Key", nameof(ConnectingInstanceList));
            ConnectingInstanceList.WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteStartElement(nameof(InstanceSourceSettingsBase));
            base.WriteXml(writer);
            writer.WriteEndElement();
        }

        #endregion
    }
}

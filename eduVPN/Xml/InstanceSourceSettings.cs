/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// Wrapper to automate serialization of various InstanceSourceSettingsBase derived classes
    /// </summary>
    public class InstanceSourceSettings : IXmlSerializable
    {
        #region Properties

        /// <summary>
        /// Instance source
        /// </summary>
        public InstanceSourceSettingsBase InstanceSource { get; set; }

        #endregion

        #region IXmlSerializable Support

        public XmlSchema GetSchema()
        {
            return null;
        }

        public virtual void ReadXml(XmlReader reader)
        {
            InstanceSource = null;

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader["Key"] == nameof(InstanceSource))
                    {
                        switch (reader.Name)
                        {
                            case nameof(InstanceSourceSettingsBase): InstanceSource = new InstanceSourceSettingsBase(); break;
                            case nameof(LocalInstanceSourceSettings): InstanceSource = new LocalInstanceSourceSettings(); break;
                            case nameof(FederatedInstanceSourceSettings): InstanceSource = new FederatedInstanceSourceSettings(); break;
                            case nameof(DistributedInstanceSourceSettings): InstanceSource = new DistributedInstanceSourceSettings(); break;
                        }

                        if (InstanceSource != null)
                        {
                            // Read element.
                            InstanceSource.ReadXml(reader);
                        }
                        else
                        {
                            // Skip unknown element.
                            var name = reader.Name;
                            while (reader.Read() &&
                                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == name)) ;
                        }
                    }
                }
            }
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            if (InstanceSource != null)
            {
                writer.WriteStartElement(InstanceSource.GetType().Name);
                writer.WriteAttributeString("Key", nameof(InstanceSource));
                InstanceSource.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}

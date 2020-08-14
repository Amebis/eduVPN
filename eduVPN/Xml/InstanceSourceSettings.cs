/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// Wrapper to automate serialization of various InstanceSourceSettingsBase derived classes
    /// </summary>
    [Obsolete]
    public class InstanceSourceSettings : IXmlSerializable
    {
        #region Properties

        /// <summary>
        /// Instance source
        /// </summary>
        public InstanceSourceSettingsBase InstanceSource { get; set; }

        #endregion

        #region IXmlSerializable Support

        /// <summary>
        /// This method is reserved and should not be used.
        /// </summary>
        /// <returns><c>null</c></returns>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="XmlReader"/> stream from which the object is deserialized.</param>
        public virtual void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement)
                return;

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

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> stream to which the object is serialized.</param>
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

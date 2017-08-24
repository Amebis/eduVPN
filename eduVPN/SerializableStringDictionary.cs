/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN
{
    /// <summary>
    /// Serializable string dictionary
    /// </summary>
    public class SerializableStringDictionary : StringDictionary, IXmlSerializable
    {
        #region IXmlSerializable Support

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            Clear();

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "SerializableStringDictionary"))
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "DictionaryEntry")
                {
                    var name = reader["Key"];
                    if (name == null)
                        throw new FormatException();

                    var value = reader["Value"];
                    this[name] = value;
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (DictionaryEntry entry in this)
            {
                writer.WriteStartElement("DictionaryEntry");
                writer.WriteAttributeString("Key", (string)entry.Key);
                writer.WriteAttributeString("Value", (string)entry.Value);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}

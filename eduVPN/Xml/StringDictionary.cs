/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable string dictionary
    /// </summary>
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "DCS does not support IXmlSerializable types that are also marked as [Serializable]")]
    public class StringDictionary : Dictionary<string, string>, IXmlSerializable
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
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "DictionaryEntry")
                {
                    var key = reader["Key"];
                    if (key == null)
                        throw new FormatException();

                    var value = reader["Value"];
                    this[key] = value;
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var entry in this)
            {
                writer.WriteStartElement("DictionaryEntry");
                writer.WriteAttributeString("Key", entry.Key);
                writer.WriteAttributeString("Value", entry.Value);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}

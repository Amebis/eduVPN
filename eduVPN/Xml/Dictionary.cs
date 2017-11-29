/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable dictionary of serializable objects
    /// </summary>
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "DCS does not support IXmlSerializable types that are also marked as [Serializable]")]
    public class Dictionary<T> : System.Collections.Generic.Dictionary<string, T>, IXmlSerializable where T : IXmlSerializable, new()
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
                var element = new T();
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == element.GetType().Name)
                {
                    var key = reader["Key"];
                    if (key == null)
                        throw new FormatException();

                    element.ReadXml(reader);
                    Add(key, element);
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var entry in this)
            {
                writer.WriteStartElement(entry.Value.GetType().Name);
                writer.WriteAttributeString("Key", entry.Key);
                entry.Value.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}

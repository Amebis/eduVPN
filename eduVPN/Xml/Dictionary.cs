/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
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
    public class Dictionary<T> : System.Collections.Generic.Dictionary<string, T>, IXmlSerializable where T : IXmlSerializable, new()
    {
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
        public void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement)
                return;

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

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> stream to which the object is serialized.</param>
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

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable server reference dictionary
    /// </summary>
    public class UriList : List<Uri>, IXmlSerializable
    {
        #region Constructors

        /// <summary>
        /// Constructs a list
        /// </summary>
        public UriList()
        {
        }

        /// <summary>
        /// Constructs a list
        /// </summary>
        /// <param name="list">The collection whose elements are copied to the new list</param>
        public UriList(IEnumerable<Uri> list) :
            base(list)
        {
        }

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
        public void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement)
                return;

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Uri")
                    Add(new Uri(reader["AbsoluteUri"]));
            }
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> stream to which the object is serialized.</param>
        public void WriteXml(XmlWriter writer)
        {
            foreach (var el in this)
            {
                writer.WriteStartElement("Uri");
                writer.WriteAttributeString("AbsoluteUri", el.AbsoluteUri);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}

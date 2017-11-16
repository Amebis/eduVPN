/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable instance reference list
    /// </summary>
    public class InstanceRefList : List<InstanceRef>, IXmlSerializable
    {
        #region Constructors

        /// <summary>
        /// Constructs a list
        /// </summary>
        public InstanceRefList()
        { }

        /// <summary>
        /// Constructs a list
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list</param>
        public InstanceRefList(IEnumerable<InstanceRef> collection) :
            base(collection)
        { }

        #endregion

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
                if (reader.NodeType == XmlNodeType.Element && reader.Name == nameof(InstanceRef))
                {
                    var el = new InstanceRef();
                    el.ReadXml(reader);
                    Add(el);
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var el in this)
            {
                writer.WriteStartElement(nameof(InstanceRef));
                el.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}

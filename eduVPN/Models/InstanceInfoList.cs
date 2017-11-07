/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Models
{
    /// <summary>
    /// VPN configuration list
    /// </summary>
    public class InstanceInfoList : ObservableCollection<InstanceInfo>, IXmlSerializable
    {
        #region Constructors

        /// <summary>
        /// Constructs a VPN configuration list
        /// </summary>
        public InstanceInfoList() :
            base()
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
                if (reader.NodeType == XmlNodeType.Element && reader.Name == nameof(InstanceInfo))
                {
                    var instance = new InstanceInfo();
                    instance.ReadXml(reader);
                    Add(instance);
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var instance in this)
            {
                writer.WriteStartElement(nameof(InstanceInfo));
                instance.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}

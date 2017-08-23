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

namespace eduVPN.Models
{
    /// <summary>
    /// Dictionary of instance specific settings to persist accross client sessions
    /// </summary>
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "DCS does not support IXmlSerializable types that are also marked as [Serializable]")]
    public class InstanceSettingsDictionary : Dictionary<string, InstanceSettings>, IXmlSerializable
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
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "InstanceSettingsDictionary"))
            {
                var key = reader["Key"];
                if (key == null)
                    throw new FormatException();

                var instance = new InstanceSettings();
                instance.ReadXml(reader);
                Add(key, instance);
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var instance in this)
            {
                writer.WriteStartElement("InstanceSettings");
                writer.WriteAttributeString("Key", instance.Key);
                instance.Value.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}

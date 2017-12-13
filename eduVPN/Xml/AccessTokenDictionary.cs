/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/


using eduOAuth;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable dictionary of OAuth access tokens
    /// </summary>
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "DCS does not support IXmlSerializable types that are also marked as [Serializable]")]
    public class AccessTokenDictionary : System.Collections.Generic.Dictionary<string, AccessToken>, IXmlSerializable
    {
        #region IXmlSerializable Support

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement)
                return;

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "AccessToken")
                {
                    var key = reader["Key"];
                    if (key == null)
                        throw new FormatException();

                    // Carefully decode access token as it might be damaged or encrypted using another session key.
                    try { this[key] = AccessToken.FromBase64String(reader["Value"]); }
                    catch { }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var entry in this)
            {
                writer.WriteStartElement("AccessToken");
                writer.WriteAttributeString("Key", entry.Key);
                writer.WriteAttributeString("Value", entry.Value.ToBase64String());
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}

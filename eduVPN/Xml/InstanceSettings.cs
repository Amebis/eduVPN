/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable instance specific settings
    /// </summary>
    public class InstanceSettings : IXmlSerializable
    {
        #region Properties

        /// <summary>
        /// Instance specific client certificate hash
        /// </summary>
        public byte[] ClientCertificateHash { get; set; }

        #endregion

        #region IXmlSerializable Support

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            string v;

            ClientCertificateHash = (v = reader[nameof(ClientCertificateHash)]) != null ? ClientCertificateHash = v.FromHexToBin() : null;
        }

        public void WriteXml(XmlWriter writer)
        {
            if (ClientCertificateHash != null)
            {
                writer.WriteStartAttribute(nameof(ClientCertificateHash));
                writer.WriteBinHex(ClientCertificateHash, 0, ClientCertificateHash.Length);
                writer.WriteEndAttribute();
            }
        }

        #endregion
    }
}

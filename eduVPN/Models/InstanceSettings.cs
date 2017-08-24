/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Models
{
    /// <summary>
    /// Instance specific settings to persist accross client sessions
    /// </summary>
    public class InstanceSettings : IXmlSerializable
    {
        #region Properties

        /// <summary>
        /// Instance specific client certificate hash
        /// </summary>
        public byte[] ClientCertificateHash
        {
            get { return _cert_hash; }
            set { _cert_hash = value; }
        }
        private byte[] _cert_hash;

        #endregion

        #region IXmlSerializable Support

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            string v;

            ClientCertificateHash = (v = reader["ClientCertificateHash"]) != null ? ClientCertificateHash = FromHexToBin(v) : null;
        }

        public void WriteXml(XmlWriter writer)
        {
            if (ClientCertificateHash != null)
            {
                writer.WriteStartAttribute("ClientCertificateHash");
                writer.WriteBinHex(ClientCertificateHash, 0, ClientCertificateHash.Length);
                writer.WriteEndAttribute();
            }
        }

        private static byte[] FromHexToBin(string hex)
        {
            var result = new List<byte>();
            byte x = 0xff;
            foreach (var c in hex)
            {
                byte n;
                if ('0' <= c && c <= '9') n = (byte)(c - '0');
                else if ('A' <= c && c <= 'F') n = (byte)(c - 'A' + 10);
                else if ('a' <= c && c <= 'f') n = (byte)(c - 'a' + 10);
                else continue;

                if ((x & 0xf) != 0)
                    x = (byte)(n << 4);
                else
                {
                    result.Add((byte)(x | n));
                    x = 0xff;
                }
            }

            return result.ToArray();
        }

        #endregion
    }
}

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
    /// Serializable resource reference with public key for verification
    /// </summary>
    public class ResourceRef : IXmlSerializable
    {
        #region Properties

        /// <summary>
        /// Resource URI
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        /// Ed25519 Public key
        /// </summary>
        public byte[] PublicKey { get; set; }

        #endregion

        #region IXmlSerializable Support

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            string v;

            Uri = !string.IsNullOrWhiteSpace(v = reader[nameof(Uri)]) ? new Uri(v) : null;
            PublicKey = !string.IsNullOrWhiteSpace(v = reader[nameof(PublicKey)]) ? PublicKey = Convert.FromBase64String(v) : null;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(nameof(Uri), Uri.AbsoluteUri);
            writer.WriteAttributeString(nameof(PublicKey), Convert.ToBase64String(PublicKey));
        }

        #endregion
    }
}

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Diagnostics;
using System.Reflection;
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
        #region Fields

        /// <summary>
        /// Base URI to be used for reading relative URIs
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Uri _assembly_uri = new Uri(Assembly.GetExecutingAssembly().Location);

        #endregion

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
            string v;

            Uri = !String.IsNullOrWhiteSpace(v = reader[nameof(Uri)]) ? new Uri(_assembly_uri, v) : null;
            PublicKey = !String.IsNullOrWhiteSpace(v = reader[nameof(PublicKey)]) ? PublicKey = Convert.FromBase64String(v) : null;
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> stream to which the object is serialized.</param>
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(nameof(Uri), Uri.AbsoluteUri);
            writer.WriteAttributeString(nameof(PublicKey), Convert.ToBase64String(PublicKey));
        }

        #endregion
    }
}

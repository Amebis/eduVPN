/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Properties;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable resource reference with public keys for verification
    /// </summary>
    [Serializable]
    public class ResourceRef : IXmlSerializable, IRegistrySerializable, ISerializable
    {
        #region Fields

        /// <summary>
        /// Base URI to be used for reading relative URLs
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Uri AssemblyUri = new Uri(Assembly.GetExecutingAssembly().Location);

        #endregion

        #region Properties

        /// <summary>
        /// Resource URI
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        /// Minisign public keys
        /// </summary>
        public MinisignPublicKeyDictionary PublicKeys { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an object
        /// </summary>
        public ResourceRef()
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
            string v;

            Uri = !string.IsNullOrWhiteSpace(v = reader[nameof(Uri)]) ? new Uri(AssemblyUri, v) : null;

            if (reader.IsEmptyElement)
                return;

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == nameof(MinisignPublicKeyDictionary))
                {
                    if (reader["Key"] == nameof(PublicKeys))
                    {
                        PublicKeys = new MinisignPublicKeyDictionary();
                        PublicKeys.ReadXml(reader);
                    }
                }
            }
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> stream to which the object is serialized.</param>
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(nameof(Uri), Uri.AbsoluteUri);

            if (PublicKeys != null)
            {
                writer.WriteStartElement(nameof(MinisignPublicKeyDictionary));
                writer.WriteAttributeString("Key", nameof(PublicKeys));
                PublicKeys.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        #endregion

        #region IRegistrySerializable Support

        public bool ReadRegistry(RegistryKey key, string name)
        {
            if (key.GetValue(name) is string v)
            {
                Uri = !string.IsNullOrEmpty(v) ? new Uri(AssemblyUri, v) : null;

                var pk = new MinisignPublicKeyDictionary();
                if (pk.ReadRegistry(key, name + "PublicKeys"))
                    PublicKeys = pk;

                return true;
            }
            return false;
        }

        #endregion

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected ResourceRef(SerializationInfo info, StreamingContext context)
        {
            Uri = (Uri)info.GetValue(nameof(PublicKeys), typeof(Uri));
            PublicKeys = (MinisignPublicKeyDictionary)info.GetValue(nameof(PublicKeys), typeof(MinisignPublicKeyDictionary));
        }

        /// <inheritdoc/>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Uri), Uri);
            info.AddValue(nameof(PublicKeys), PublicKeys);
        }

        #endregion
    }
}

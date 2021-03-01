/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable Minisign public key list
    /// </summary>
    [Serializable]
    public class MinisignPublicKeyDictionary : Dictionary<ulong, byte[]>, IXmlSerializable, IRegistrySerializable
    {
        #region Constructors

        /// <summary>
        /// Constructs a dictionary
        /// </summary>
        public MinisignPublicKeyDictionary()
        {
        }

        /// <summary>
        /// Constructs a dictionary
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new dictionary</param>
        public MinisignPublicKeyDictionary(IDictionary<ulong, byte[]> collection) :
            base(collection)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a public key
        /// </summary>
        /// <param name="public_key">Base64 encoded public key to add</param>
        public void Add(string public_key)
        {
            using (var s = new MemoryStream(Convert.FromBase64String(public_key), false))
            using (var r = new BinaryReader(s))
            {
                if (r.ReadChar() != 'E' || r.ReadChar() != 'd')
                    throw new ArgumentException(Resources.Strings.ErrorUnsupportedMinisignPublicKey);
                ulong keyId = r.ReadUInt64();
                var key = new byte[32];
                if (r.Read(key, 0, 32) != 32)
                    throw new ArgumentException(Resources.Strings.ErrorInvalidMinisignPublicKey);
                if (ContainsKey(keyId))
                    throw new ArgumentException(string.Format(Resources.Strings.ErrorDuplicateMinisignPublicKey, keyId));
                Add(keyId, key);
            }
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
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "PublicKey")
                {
                    while (reader.Read() &&
                        !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "PublicKey"))
                        if (reader.NodeType == XmlNodeType.Text)
                            Add(reader.Value);
                }
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
                writer.WriteStartElement("PublicKey");
                using (var s = new MemoryStream(42))
                {
                    using (var w = new BinaryWriter(s))
                    {
                        w.Write('E');
                        w.Write('d');
                        w.Write(el.Key);
                        w.Write(el.Value);
                    }
                    writer.WriteBase64(s.GetBuffer(), 0, (int)s.Length);
                }
                writer.WriteEndElement();
            }
        }

        #endregion

        #region IRegistrySerializable Support

        public bool ReadRegistry(RegistryKey key, string name)
        {
            if (key.GetValue(name) is string[] v)
            {
                foreach (var str in v)
                    Add(str);
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
        protected MinisignPublicKeyDictionary(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}

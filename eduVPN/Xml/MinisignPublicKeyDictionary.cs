/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable Minisign public key list
    /// </summary>
    public class MinisignPublicKeyDictionary : Dictionary<ulong, byte[]>, IXmlSerializable
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
        /// <param name="collection">The collection whose elements are copied to the new list</param>
        public MinisignPublicKeyDictionary(IDictionary<ulong, byte[]> collection) :
            base(collection)
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
            if (reader.IsEmptyElement)
                return;

            ulong key_id;
            var key = new byte[32];
            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "MinisignPublicKey")
                {
                    while (reader.Read() &&
                        !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "MinisignPublicKey"))
                    {
                        if (reader.NodeType == XmlNodeType.Text)
                        {
                            using (var s = new MemoryStream(Convert.FromBase64String(reader.Value), false))
                            using (var r = new BinaryReader(s))
                            {
                                if (r.ReadChar() != 'E' || r.ReadChar() != 'd')
                                    throw new ArgumentException(Resources.Strings.ErrorUnsupportedMinisignPublicKey);
                                key_id = r.ReadUInt64();
                                if (r.Read(key, 0, 32) != 32)
                                    throw new ArgumentException(Resources.Strings.ErrorInvalidMinisignPublicKey);
                            }
                            if (ContainsKey(key_id))
                                throw new ArgumentException(String.Format(Resources.Strings.ErrorDuplicateMinisignPublicKey, key_id));
                            Add(key_id, key);
                        }
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
            foreach (var el in this)
            {
                writer.WriteStartElement("MinisignPublicKey");
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
    }
}

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
    public class MinisignPublicKeyDictionary : HashSet<MinisignPublicKey>, IXmlSerializable, IRegistrySerializable
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
        public MinisignPublicKeyDictionary(IEnumerable<MinisignPublicKey> collection) :
            base(collection)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a public key
        /// </summary>
        /// <param name="public_key">Base64 encoded public key to add</param>
        /// <param name="supportedAlgorithms">Bitwise mask of supported Minisign algorithms</param>
        public void Add(string public_key, MinisignPublicKey.AlgorithmMask supportedAlgorithms = MinisignPublicKey.AlgorithmMask.All)
        {
            Add(new MinisignPublicKey(public_key, supportedAlgorithms));
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
                    string v;
                    var supportedAlgorithms = ((v = reader["SupportedAlgorithms"]) != null) ? (MinisignPublicKey.AlgorithmMask)int.Parse(v) : MinisignPublicKey.AlgorithmMask.All;
                    while (reader.Read() &&
                        !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "PublicKey"))
                        if (reader.NodeType == XmlNodeType.Text)
                            Add(reader.Value, supportedAlgorithms);
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
                writer.WriteAttributeString("SupportedAlgorithms", ((int)el.SupportedAlgorithms).ToString());
                var data = el.Data;
                writer.WriteBase64(data, 0, data.Length);
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
                {
                    var fields = str.Split('|');
                    if (fields.Length == 1)
                        Add(fields[0]);
                    else if (fields.Length >= 2)
                        Add(fields[0], (MinisignPublicKey.AlgorithmMask)int.Parse(fields[1]));
                }
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

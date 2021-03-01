/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// Instance settings
    /// </summary>
    public class InstanceSettings : IXmlSerializable
    {
        #region Properties

        /// <summary>
        /// Last username used
        /// </summary>
        public string LastUsername { get; set; }

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
        public virtual void ReadXml(XmlReader reader)
        {
            LastUsername = reader[nameof(LastUsername)];
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> stream to which the object is serialized.</param>
        public virtual void WriteXml(XmlWriter writer)
        {
            if (LastUsername != null)
                writer.WriteAttributeString(nameof(LastUsername), LastUsername);
        }

        #endregion
    }
}

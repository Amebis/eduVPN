/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable instance reference
    /// </summary>
    [Obsolete]
    public class InstanceRef : IXmlSerializable
    {
        #region Properties

        /// <summary>
        /// Instance base URI
        /// </summary>
        public Uri Base { get; private set; }

        /// <summary>
        /// Popularity factor in the [0.0, 1.0] range (default 1.0)
        /// </summary>
        public float Popularity { get; private set; } = 1.0f;

        /// <summary>
        /// List of profiles
        /// </summary>
        public ProfileRefList Profiles { get; private set; }

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

            Base = (v = reader[nameof(Base)]) != null ? new Uri(v) : null;
            Popularity = (v = reader[nameof(Popularity)]) != null && float.TryParse(v, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var v_popularity) ? Popularity = v_popularity : 1.0f;

            if (reader.IsEmptyElement)
                return;

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == nameof(ProfileRefList))
                {
                    if (reader["Key"] == nameof(Profiles))
                    {
                        Profiles = new ProfileRefList();
                        Profiles.ReadXml(reader);
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
            writer.WriteAttributeString(nameof(Base), Base.AbsoluteUri);
            writer.WriteAttributeString(nameof(Popularity), Popularity.ToString(CultureInfo.InvariantCulture));

            if (Profiles != null)
            {
                writer.WriteStartElement(nameof(ProfileRefList));
                writer.WriteAttributeString("Key", nameof(Profiles));
                Profiles.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}

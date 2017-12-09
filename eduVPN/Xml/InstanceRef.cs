/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
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
    public class InstanceRef : IXmlSerializable
    {
        #region Properties

        /// <summary>
        /// Instance base URI
        /// </summary>
        public Uri Base { get; set; }

        /// <summary>
        /// Popularity factor in the [0.0, 1.0] range (default 1.0)
        /// </summary>
        public float Popularity { get; set; } = 1.0f;

        /// <summary>
        /// List of profiles
        /// </summary>
        public ProfileRefList ProfileList { get; set; }

        #endregion

        #region IXmlSerializable Support

        public XmlSchema GetSchema()
        {
            return null;
        }

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
                    if (reader["Key"] == nameof(ProfileList))
                    {
                        ProfileList = new ProfileRefList();
                        ProfileList.ReadXml(reader);
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(nameof(Base), Base.AbsoluteUri);
            writer.WriteAttributeString(nameof(Popularity), Popularity.ToString(CultureInfo.InvariantCulture));

            if (ProfileList != null)
            {
                writer.WriteStartElement(nameof(ProfileRefList));
                writer.WriteAttributeString("Key", nameof(ProfileList));
                ProfileList.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}

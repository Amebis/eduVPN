/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
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
    /// Serializable profile reference
    /// </summary>
    public class ProfileRef : IXmlSerializable
    {
        #region Properties

        /// <summary>
        /// Profile ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Profile name to display in GUI
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Popularity factor in the [0.0, 1.0] range (default 1.0)
        /// </summary>
        public float Popularity { get; set; } = 1.0f;

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

            Id = reader["ID"];
            DisplayName = !String.IsNullOrWhiteSpace(v = reader[nameof(DisplayName)]) ? v : null;
            Popularity = (v = reader[nameof(Popularity)]) != null && float.TryParse(v, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var v_popularity) ? Popularity = v_popularity : 1.0f;
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> stream to which the object is serialized.</param>
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("ID", Id);
            if (DisplayName != null)
                writer.WriteAttributeString(nameof(DisplayName), DisplayName);
            writer.WriteAttributeString(nameof(Popularity), Popularity.ToString(CultureInfo.InvariantCulture));
        }

        #endregion
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Models
{
    /// <summary>
    /// VPN configuration as persisted to settings base class
    /// </summary>
    public class VPNConfigurationSettings : IXmlSerializable
    {
        #region Properties

        /// <summary>
        /// Access token
        /// </summary>
        public AccessToken AccessToken { get; set; }

        /// <summary>
        /// Popularity factor (default 1.0)
        /// </summary>
        public float Popularity { get; set; } = 1.0f;

        #endregion

        #region Methods

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as VPNConfigurationSettings;
            if (AccessToken == null && other.AccessToken != null ||
                AccessToken != null && other.AccessToken == null)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ (AccessToken != null ? 1 : 0);
        }

        #endregion

        #region IXmlSerializable Support

        public XmlSchema GetSchema()
        {
            return null;
        }

        public virtual void ReadXml(XmlReader reader)
        {
            string v;

            Popularity = (v = reader["Popularity"]) != null && float.TryParse(v, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var v_popularity) ? Popularity = v_popularity : 1.0f;
            AccessToken = (v = reader["AccessToken"]) != null ? AccessToken.FromBase64String(v) : null;
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("Popularity", Popularity.ToString(CultureInfo.InvariantCulture));

            if (AccessToken != null)
            {
                writer.WriteStartAttribute("AccessToken");
                writer.WriteString(AccessToken.ToBase64String());
                writer.WriteEndAttribute();
            }
        }

        #endregion
    }
}

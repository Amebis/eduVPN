/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
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

        /// <summary>
        /// Last 2-Factor Authentication method used
        /// </summary>
        public string LastTwoFactorAuthenticationMethod { get; set; }

        /// <summary>
        /// Last 2-Factor Authentication response time
        /// </summary>
        /// <remarks><c>null</c> if none.</remarks>
        public DateTime? LastTwoFactorAuthenticationResponse { get; set; }

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
            string v;

            LastUsername = reader[nameof(LastUsername)];
            LastTwoFactorAuthenticationMethod = reader[nameof(LastTwoFactorAuthenticationMethod)];
            LastTwoFactorAuthenticationResponse = (v = reader[nameof(LastTwoFactorAuthenticationResponse)]) != null && DateTime.TryParse(v, out var last_response) ? (DateTime?)last_response : null;
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> stream to which the object is serialized.</param>
        public virtual void WriteXml(XmlWriter writer)
        {
            if (LastUsername != null)
                writer.WriteAttributeString(nameof(LastUsername), LastUsername);

            if (LastTwoFactorAuthenticationMethod != null)
                writer.WriteAttributeString(nameof(LastTwoFactorAuthenticationMethod), LastTwoFactorAuthenticationMethod);

            if (LastTwoFactorAuthenticationResponse != null)
                writer.WriteAttributeString(nameof(LastTwoFactorAuthenticationResponse), LastTwoFactorAuthenticationResponse.Value.ToString("o"));
        }

        #endregion
    }
}

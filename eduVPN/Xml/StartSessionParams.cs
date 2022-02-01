/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable <see cref="ViewModels.Pages.ConnectionPage.StartSession"/> command parameter set
    /// </summary>
    [Obsolete]
    public class StartSessionParams : IXmlSerializable
    {
        #region Properties

        /// <summary>
        /// Connecting server base URI
        /// </summary>
        public Uri ConnectingServer { get; private set; }

        /// <summary>
        /// Profile identifier
        /// </summary>
        public string ProfileId { get; private set; }

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

            ConnectingServer = (v = reader[nameof(ConnectingServer)]) != null || (v = reader["ConnectingInstance"]) != null || (v = reader["Instance"]) != null ? new Uri(v) : null;
            ProfileId = (v = reader[nameof(ProfileId)]) != null || (v = reader["Id"]) != null ? v : null;
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> stream to which the object is serialized.</param>
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(nameof(ConnectingServer), ConnectingServer.AbsoluteUri);
            writer.WriteAttributeString(nameof(ProfileId), ProfileId);
        }

        #endregion
    }
}

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable <see cref="eduVPN.ViewModels.Windows.ConnectWizard.StartSession"/> command parameter set
    /// </summary>
    public class StartSessionParams : IXmlSerializable
    {
        #region Properties

        /// <summary>
        /// Instance source
        /// </summary>
        public Models.InstanceSourceType InstanceSourceType { get; set; }

        /// <summary>
        /// Instance base URI
        /// </summary>
        public Uri Instance { get; set; }

        /// <summary>
        /// Profile ID
        /// </summary>
        public string ID { get; set; }

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

            Instance = (v = reader[nameof(Instance)]) != null ? new Uri(v) : null;
            ID = reader[nameof(ID)];
            InstanceSourceType = (v = reader[nameof(InstanceSourceType)]) != null ? (Models.InstanceSourceType)Enum.Parse(typeof(Models.InstanceSourceType), v) : Models.InstanceSourceType._unknown;
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> stream to which the object is serialized.</param>
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(nameof(Instance), Instance.AbsoluteUri);
            writer.WriteAttributeString(nameof(ID), ID);
            writer.WriteAttributeString(nameof(InstanceSourceType), Enum.GetName(typeof(Models.InstanceSourceType), InstanceSourceType));
        }

        #endregion
    }
}

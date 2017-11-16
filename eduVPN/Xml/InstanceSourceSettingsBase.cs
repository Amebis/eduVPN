/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable instance source base class
    /// </summary>
    public class InstanceSourceSettingsBase : IXmlSerializable
    {
        #region Properties

        /// <summary>
        /// Last connecting instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Uri ConnectingInstance { get; set; }

        #endregion

        #region IXmlSerializable Support

        public XmlSchema GetSchema()
        {
            return null;
        }

        public virtual void ReadXml(XmlReader reader)
        {
            string v;

            ConnectingInstance = (v = reader[nameof(ConnectingInstance)]) != null ? new Uri(v) : null;
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            if (ConnectingInstance != null)
                writer.WriteAttributeString(nameof(ConnectingInstance), ConnectingInstance.AbsoluteUri);
        }

        #endregion
    }
}

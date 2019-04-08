/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Xml;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable instance source using distributed authentication
    /// </summary>
    /// <remarks>
    /// Access token from any instance can be used by any other instance.
    /// </remarks>
    public class DistributedInstanceSourceSettings : InstanceSourceSettingsBase
    {
        #region Properties

        /// <summary>
        /// Authenticating instance
        /// </summary>
        public Uri AuthenticatingInstance { get; set; }

        #endregion

        #region IXmlSerializable Support

        /// <inheritdoc/>
        public override void ReadXml(XmlReader reader)
        {
            string v;

            AuthenticatingInstance = (v = reader[nameof(AuthenticatingInstance)]) != null ? new Uri(v) : null;

            if (reader.IsEmptyElement)
                return;

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case nameof(InstanceSourceSettingsBase):
                            base.ReadXml(reader);
                            break;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void WriteXml(XmlWriter writer)
        {
            if (AuthenticatingInstance != null)
                writer.WriteAttributeString(nameof(AuthenticatingInstance), AuthenticatingInstance.AbsoluteUri);

            writer.WriteStartElement(nameof(InstanceSourceSettingsBase));
            base.WriteXml(writer);
            writer.WriteEndElement();
        }

        #endregion
    }
}

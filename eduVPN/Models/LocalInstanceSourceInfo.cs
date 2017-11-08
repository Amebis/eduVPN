/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Xml;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN list of instances using local authentication
    /// </summary>
    /// <remarks>
    /// Access token is specific to each instance and cannot be used by other instances.
    /// </remarks>
    public class LocalInstanceSourceInfo : InstanceSourceInfo
    {
        #region Properties

        /// <inheritdoc/>
        public override InstanceInfoList ConnectingInstanceList
        {
            get { return _connecting_instance_list; }
            set { SetProperty(ref _connecting_instance_list, value); }
        }
        private InstanceInfoList _connecting_instance_list = new InstanceInfoList();

        #endregion

        #region IXmlSerializable Support

        /// <inheritdoc/>
        public override void ReadXml(XmlReader reader)
        {
            ConnectingInstanceList.Clear();

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case nameof(InstanceInfoList):
                            if (reader["Key"] == nameof(ConnectingInstanceList))
                                ConnectingInstanceList.ReadXml(reader);
                            break;

                        case nameof(InstanceSourceInfo):
                            base.ReadXml(reader);
                            break;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(nameof(InstanceInfoList));
            writer.WriteAttributeString("Key", nameof(ConnectingInstanceList));
            ConnectingInstanceList.WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteStartElement(nameof(InstanceSourceInfo));
            base.WriteXml(writer);
            writer.WriteEndElement();
        }

        #endregion
    }
}

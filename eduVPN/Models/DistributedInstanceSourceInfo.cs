/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Xml;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN list of instances using distributed authentication
    /// </summary>
    /// <remarks>
    /// Access token from any instance can be used by any other instance.
    /// </remarks>
    public class DistributedInstanceSourceInfo : InstanceSourceInfo
    {
        #region Properties

        /// <summary>
        /// Authenticating instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public override InstanceInfo AuthenticatingInstance
        {
            get { return _authenticating_instance; }
            set { SetProperty(ref _authenticating_instance, value); }
        }
        private InstanceInfo _authenticating_instance;

        #endregion

        #region IXmlSerializable Support

        /// <inheritdoc/>
        public override void ReadXml(XmlReader reader)
        {
            AuthenticatingInstance = null;

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case nameof(InstanceInfo):
                            if (reader["Key"] == nameof(AuthenticatingInstance))
                            {
                                AuthenticatingInstance = new InstanceInfo();
                                AuthenticatingInstance.ReadXml(reader);
                            }

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
            if (AuthenticatingInstance != null)
            {
                writer.WriteStartElement(nameof(InstanceInfo));
                writer.WriteAttributeString("Key", nameof(AuthenticatingInstance));
                AuthenticatingInstance.WriteXml(writer);
                writer.WriteEndElement();
            }

            writer.WriteStartElement(nameof(InstanceSourceInfo));
            base.WriteXml(writer);
            writer.WriteEndElement();
        }

        #endregion
    }
}

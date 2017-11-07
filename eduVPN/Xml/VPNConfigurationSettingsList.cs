/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// VPN configuration list
    /// </summary>
    [Obsolete]
    public class VPNConfigurationSettingsList : List<VPNConfigurationSettings>, IXmlSerializable
    {
        #region Constructors

        /// <summary>
        /// Constructs a VPN configuration list
        /// </summary>
        public VPNConfigurationSettingsList() :
            base()
        { }

        /// <summary>
        /// Constructs a VPN configuration list that is empty and has the specified initial capacity
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        public VPNConfigurationSettingsList(int capacity) :
            base(capacity)
        { }

        #endregion

        #region IXmlSerializable Support

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            Clear();

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    // Determine which type of configuration was read.
                    VPNConfigurationSettings cfg = null;
                    switch (reader.Name)
                    {
                        case nameof(VPNConfigurationSettings): cfg = new VPNConfigurationSettings(); break;
                        case nameof(LocalVPNConfigurationSettings): cfg = new LocalVPNConfigurationSettings(); break;
                        case nameof(FederatedVPNConfigurationSettings): cfg = new FederatedVPNConfigurationSettings(); break;
                        case nameof(DistributedVPNConfigurationSettings): cfg = new DistributedVPNConfigurationSettings(); break;
                    }

                    if (cfg != null)
                    {
                        // Read configuration.
                        cfg.ReadXml(reader);
                        Add(cfg);
                    }
                    else
                    {
                        // Skip unknown configuration.
                        var name = reader.Name;
                        while (reader.Read() &&
                            !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == name));
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var cfg in this)
            {
                writer.WriteStartElement(cfg.GetType().Name);
                cfg.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}

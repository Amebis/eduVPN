/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Models
{
    /// <summary>
    /// VPN configuration list
    /// </summary>
    public class VPNConfigurationSettingsList : List<VPNConfigurationSettings>, IXmlSerializable
    {
        #region IXmlSerializable Support

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            Clear();

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "VPNConfigurationSettingsList"))
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    // Determine which type of configuration was read.
                    VPNConfigurationSettings cfg = null;
                    switch (reader.Name)
                    {
                        case "VPNConfigurationSettings": cfg = new VPNConfigurationSettings(); break;
                        case "LocalVPNConfigurationSettings": cfg = new LocalVPNConfigurationSettings(); break;
                        case "FederatedVPNConfigurationSettings": cfg = new FederatedVPNConfigurationSettings(); break;
                        case "DistributedVPNConfigurationSettings": cfg = new DistributedVPNConfigurationSettings(); break;
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

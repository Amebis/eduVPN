﻿/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
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
        /// Constructs a VPN configuration list
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        public VPNConfigurationSettingsList(int capacity) :
            base(capacity)
        { }

        /// <summary>
        /// Constructs a VPN configuration list
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list</param>
        public VPNConfigurationSettingsList(IEnumerable<VPNConfigurationSettings> collection) :
            base(collection)
        { }

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
            if (reader.IsEmptyElement)
                return;

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

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> stream to which the object is serialized.</param>
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

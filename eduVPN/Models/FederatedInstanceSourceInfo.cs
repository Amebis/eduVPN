/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Xml;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN list of instances using federated authentication
    /// </summary>
    /// <remarks>
    /// Access token is issued by a central OAuth server; all instances accept this token.
    /// </remarks>
    public class FederatedInstanceSourceInfo : DistributedInstanceSourceInfo
    {
        #region ILoadableItem Support

        /// <inheritdoc/>
        public override void Load(object obj)
        {
            base.Load(obj);

            if (obj is Dictionary<string, object> obj2)
            {
                AuthenticatingInstance = new InstanceInfo(
                    new Uri(eduJSON.Parser.GetValue<string>(obj2, "authorization_endpoint")),
                    new Uri(eduJSON.Parser.GetValue<string>(obj2, "token_endpoint")));
            }
            else
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(Dictionary<string, object>), obj.GetType());
        }

        #endregion

        #region IXmlSerializable Support

        /// <inheritdoc/>
        public override void ReadXml(XmlReader reader)
        {
            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == nameof(DistributedInstanceSourceInfo))
                    base.ReadXml(reader);
            }
        }

        /// <inheritdoc/>
        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(nameof(DistributedInstanceSourceInfo));
            base.WriteXml(writer);
            writer.WriteEndElement();
        }

        #endregion
    }
}

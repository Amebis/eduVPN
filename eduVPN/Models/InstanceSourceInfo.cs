/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN instance source base class
    /// </summary>
    public class InstanceSourceInfo : BindableBase, JSON.ILoadableItem, IXmlSerializable
    {
        #region Properties

        public InstanceInfoList InstanceList
        {
            get { return _instance_list; }
            set
            {
                if (SetProperty(ref _instance_list, value))
                    RaisePropertyChanged(nameof(ConnectingInstanceList));
            }
        }
        private InstanceInfoList _instance_list = new InstanceInfoList();

        /// <summary>
        /// Authenticating instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public virtual InstanceInfo AuthenticatingInstance
        {
            get { return ConnectingInstance; }
            set { ConnectingInstance = value; }
        }

        /// <summary>
        /// User saved instance list
        /// </summary>
        public virtual InstanceInfoList ConnectingInstanceList
        {
            get { return _instance_list; }
            set { throw new InvalidOperationException(); }
        }

        /// <summary>
        /// Last connecting instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public InstanceInfo ConnectingInstance
        {
            get { return _connecting_instance; }
            set
            {
                if (SetProperty(ref _connecting_instance, value))
                    RaisePropertyChanged(nameof(AuthenticatingInstance));
            }
        }
        private InstanceInfo _connecting_instance;

        /// <summary>
        /// Version sequence
        /// </summary>
        public uint Sequence
        {
            get { return _sequence; }
            set { SetProperty(ref _sequence, value); }
        }
        private uint _sequence;

        /// <summary>
        /// Signature timestamp
        /// </summary>
        public DateTime? SignedAt
        {
            get { return _signed_at; }
            set { SetProperty(ref _signed_at, value); }
        }
        private DateTime? _signed_at;

        #endregion

        #region Methods

        /// <summary>
        /// Loads instance source from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>instances</c> and other optional elements</param>
        /// <returns>Instance source</returns>
        public static InstanceSourceInfo FromJSON(Dictionary<string, object> obj)
        {
            // Parse authorization data.
            InstanceSourceInfo instance_source;
        #if INSTANCE_LIST_FORCE_LOCAL
            instance_source = new LocalInstanceSourceInfo();
        #elif INSTANCE_LIST_FORCE_DISTRIBUTED
            instance_source = new DistributedInstanceSourceInfo();
        #elif INSTANCE_LIST_FORCE_FEDERATED
            instance_source = new FederatedInstanceSourceInfo();
            obj.Add("authorization_endpoint", "https://demo.eduvpn.nl/portal/_oauth/authorize");
            obj.Add("token_endpoint"        , "https://demo.eduvpn.nl/portal/oauth.php/token");
        #else
            if (eduJSON.Parser.GetValue(obj, "authorization_type", out string authorization_type))
            {
                switch (authorization_type.ToLower())
                {
                    case "federated": instance_source = new FederatedInstanceSourceInfo(); break;
                    case "distributed": instance_source = new DistributedInstanceSourceInfo(); break;
                    default: instance_source = new LocalInstanceSourceInfo(); break; // Assume local authorization type on all other values.
                }
            }
            else
                instance_source = new LocalInstanceSourceInfo();
        #endif

            instance_source.Load(obj);
            return instance_source;
        }

        #endregion

        #region ILoadableItem Support

        /// <summary>
        /// Loads instance source from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>instances</c> and other optional elements</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public virtual void Load(object obj)
        {
            if (obj is Dictionary<string, object> obj2)
            {
                InstanceList.Clear();

                // Parse all instances listed. Don't do it in parallel to preserve the sort order.
                foreach (var el in eduJSON.Parser.GetValue<List<object>>(obj2, "instances"))
                {
                    var instance = new InstanceInfo();
                    instance.Load(el);
                    InstanceList.Add(instance);
                }

                // Parse sequence.
                Sequence = (uint)eduJSON.Parser.GetValue<int>(obj2, "seq");

                // Parse signed date.
                SignedAt = eduJSON.Parser.GetValue(obj2, "signed_at", out string signed_at) && DateTime.TryParse(signed_at, out var signed_at_date) ? signed_at_date : (DateTime?)null;
            }
            else
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(Dictionary<string, object>), obj.GetType());
        }

        #endregion

        #region IXmlSerializable Support

        public XmlSchema GetSchema()
        {
            return null;
        }

        public virtual void ReadXml(XmlReader reader)
        {
            string v;

            InstanceList.Clear();
            ConnectingInstance = null;
            Sequence = (v = reader[nameof(Sequence)]) != null && uint.TryParse(v, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var v_sequence) ? Sequence = v_sequence : 0;
            SignedAt = (v = reader[nameof(SignedAt)]) != null && DateTime.TryParse(v, out var v_signed_at) ? new DateTime?(v_signed_at) : null;

            while (reader.Read() &&
                !(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == GetType().Name))
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case nameof(InstanceInfoList):
                            if (reader["Key"] == nameof(InstanceList))
                                InstanceList.ReadXml(reader);
                            break;

                        case nameof(InstanceInfo):
                            if (reader["Key"] == nameof(ConnectingInstance))
                            {
                                ConnectingInstance = new InstanceInfo();
                                ConnectingInstance.ReadXml(reader);
                            }
                            break;
                    }
                }
            }
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(nameof(Sequence), Sequence.ToString(CultureInfo.InvariantCulture));
            if (SignedAt != null)
                writer.WriteAttributeString(nameof(SignedAt), SignedAt.Value.ToString("o"));

            writer.WriteStartElement(nameof(InstanceInfoList));
            writer.WriteAttributeString("Key", nameof(InstanceList));
            InstanceList.WriteXml(writer);
            writer.WriteEndElement();

            if (ConnectingInstance != null)
            {
                writer.WriteStartElement(nameof(InstanceInfo));
                writer.WriteAttributeString("Key", nameof(ConnectingInstance));
                ConnectingInstance.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN instance source base class
    /// </summary>
    public class InstanceSourceInfo : ObservableCollection<InstanceInfo>, JSON.ILoadableItem
    {
        #region Properties

        /// <summary>
        /// Version sequence
        /// </summary>
        public uint Sequence
        {
            get { return _sequence; }
            set { if (value != _sequence) { _sequence = value; OnPropertyChanged(new PropertyChangedEventArgs("Sequence")); } }
        }
        private uint _sequence;

        /// <summary>
        /// Signature timestamp
        /// </summary>
        public DateTime? SignedAt
        {
            get { return _signed_at; }
            set { if (value != _signed_at) { _signed_at = value; OnPropertyChanged(new PropertyChangedEventArgs("SignedAt")); } }
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
                Clear();

                // Parse all instances listed. Don't do it in parallel to preserve the sort order.
                foreach (var el in eduJSON.Parser.GetValue<List<object>>(obj2, "instances"))
                {
                    var instance = new InstanceInfo();
                    instance.Load(el);
                    Add(instance);
                }

                // Parse sequence.
                Sequence = eduJSON.Parser.GetValue(obj2, "seq", out int seq) ? (uint)seq : 0;

                // Parse signed date.
                SignedAt = eduJSON.Parser.GetValue(obj2, "signed_at", out string signed_at) && DateTime.TryParse(signed_at, out var signed_at_date) ? signed_at_date : (DateTime?)null;
            }
            else
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(Dictionary<string, object>), obj.GetType());
        }

        #endregion
    }
}

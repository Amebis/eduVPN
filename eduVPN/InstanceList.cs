/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;

namespace eduVPN
{
    /// <summary>
    /// An eduVPN list of instances = VPN service providers
    /// </summary>
    public class InstanceList : ObservableCollection<Instance>
    {
        #region Data Types

        /// <summary>
        /// Authorization type
        /// </summary>
        public enum AuthorizationType
        {
            /// <summary>
            /// Access token is specific to each instance and cannot be used by other instances.
            /// </summary>
            Local,

            /// <summary>
            /// Access token is issued by a central OAuth server; all instances accept this token.
            /// </summary>
            Federated,

            /// <summary>
            /// Access token from any instance can be used by any other instance.
            /// </summary>
            Distributed
        }

        #endregion

        #region Properties

        /// <summary>
        /// Authorization type
        /// </summary>
        public AuthorizationType AuthType { get => _auth_type; }
        private AuthorizationType _auth_type;

        /// <summary>
        /// Version sequence
        /// </summary>
        public uint Sequence { get => _sequence; }
        private uint _sequence;

        /// <summary>
        /// Signature timestamp
        /// </summary>
        public DateTime? SignedAt { get => _signed_at; }
        private DateTime? _signed_at;

        #endregion

        #region Methods

        /// <summary>
        /// Loads instance list from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>authorization_endpoint</c>, <c>token_endpoint</c> and other optional elements. All elements should be strings representing URI(s).</param>
        public void Load(Dictionary<string, object> obj)
        {
            Clear();

            // Parse all instances listed. Don't do it in parallel to preserve the sort order.
            foreach (var el in eduJSON.Parser.GetValue<List<object>>(obj, "instances"))
            {
                if (el.GetType() == typeof(Dictionary<string, object>))
                {
                    var instance = new Instance();
                    instance.Load((Dictionary<string, object>)el);
                    Add(instance);
                }
            }

            // Parse sequence.
            _sequence = eduJSON.Parser.GetValue(obj, "seq", out int seq) ? (uint)seq : 0;
            OnPropertyChanged(new PropertyChangedEventArgs("Sequence"));

            // Parse authorization data.
            if (eduJSON.Parser.GetValue(obj, "authorization_type", out string authorization_type))
            {
                switch (authorization_type.ToLower())
                {
                    case "federated": _auth_type = AuthorizationType.Federated; break;
                    case "distributed": _auth_type = AuthorizationType.Distributed; break;
                    default: _auth_type = AuthorizationType.Local; break; // Assume local authorization type on all other values.
                }
            }
            else
                _auth_type = AuthorizationType.Local;
            OnPropertyChanged(new PropertyChangedEventArgs("AuthType"));

            // Parse signed date.
            _signed_at = eduJSON.Parser.GetValue(obj, "signed_at", out string signed_at) && DateTime.TryParse(signed_at, out DateTime signed_at_date) ? signed_at_date : (DateTime?)null;
            OnPropertyChanged(new PropertyChangedEventArgs("SignedAt"));
        }

        /// <summary>
        /// Loads instance list from a JSON string
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <param name="ct">The token to monitor for cancellation requests.</param>
        public void Load(string json, CancellationToken ct = default(CancellationToken))
        {
            Load((Dictionary<string, object>)eduJSON.Parser.Parse(json, ct));
        }

        #endregion
    }
}

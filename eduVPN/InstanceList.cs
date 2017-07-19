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
        public AuthorizationType AuthType
        {
            get { return _auth_type; }
            set { if (value != _auth_type) { _auth_type = value; OnPropertyChanged(new PropertyChangedEventArgs("AuthType")); } }
        }
        private AuthorizationType _auth_type;

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
        /// Loads instance list from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>instances</c> and other optional elements.</param>
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
            Sequence = eduJSON.Parser.GetValue(obj, "seq", out int seq) ? (uint)seq : 0;

            // Parse authorization data.
            if (eduJSON.Parser.GetValue(obj, "authorization_type", out string authorization_type))
            {
                switch (authorization_type.ToLower())
                {
                    case "federated": AuthType = AuthorizationType.Federated; break;
                    case "distributed": AuthType = AuthorizationType.Distributed; break;
                    default: AuthType = AuthorizationType.Local; break; // Assume local authorization type on all other values.
                }
            }
            else
                AuthType = AuthorizationType.Local;

            // Parse signed date.
            SignedAt = eduJSON.Parser.GetValue(obj, "signed_at", out string signed_at) && DateTime.TryParse(signed_at, out DateTime signed_at_date) ? signed_at_date : (DateTime?)null;
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

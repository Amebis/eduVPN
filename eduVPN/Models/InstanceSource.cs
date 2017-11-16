/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN instance source base class
    /// </summary>
    public class InstanceSource : BindableBase, JSON.ILoadableItem
    {
        #region Properties

        /// <summary>
        /// List of all available instances
        /// </summary>
        public ObservableCollection<Instance> InstanceList { get; } = new ObservableCollection<Instance>();

        /// <summary>
        /// Authenticating instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public virtual Instance AuthenticatingInstance
        {
            get { return ConnectingInstance; }
            set { ConnectingInstance = value; }
        }

        /// <summary>
        /// User saved instance list
        /// </summary>
        public virtual ObservableCollection<Instance> ConnectingInstanceList
        {
            get { return InstanceList; }
        }

        /// <summary>
        /// Last connecting instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Instance ConnectingInstance
        {
            get { return _connecting_instance; }
            set
            {
                if (SetProperty(ref _connecting_instance, value))
                    RaisePropertyChanged(nameof(AuthenticatingInstance));
            }
        }
        private Instance _connecting_instance;

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
        public static InstanceSource FromJSON(Dictionary<string, object> obj)
        {
            // Parse authorization data.
            InstanceSource instance_source;
        #if INSTANCE_LIST_FORCE_LOCAL
            instance_source = new LocalInstanceSource();
        #elif INSTANCE_LIST_FORCE_DISTRIBUTED
            instance_source = new DistributedInstanceSource();
        #elif INSTANCE_LIST_FORCE_FEDERATED
            instance_source = new FederatedInstanceSource();
            obj.Add("authorization_endpoint", "https://demo.eduvpn.nl/portal/_oauth/authorize");
            obj.Add("token_endpoint"        , "https://demo.eduvpn.nl/portal/oauth.php/token");
        #else
            if (eduJSON.Parser.GetValue(obj, "authorization_type", out string authorization_type))
            {
                switch (authorization_type.ToLower())
                {
                    case "federated": instance_source = new FederatedInstanceSource(); break;
                    case "distributed": instance_source = new DistributedInstanceSource(); break;
                    default: instance_source = new LocalInstanceSource(); break; // Assume local authorization type on all other values.
                }
            }
            else
                instance_source = new LocalInstanceSource();
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
                    var instance = new Instance();
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
    }
}

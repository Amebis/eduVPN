/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

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
        public Instance AuthenticatingInstance
        {
            get { return _authenticating_instance; }
            set { SetProperty(ref _authenticating_instance, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Instance _authenticating_instance;

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
            set { SetProperty(ref _connecting_instance, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Instance _connecting_instance;

        /// <summary>
        /// Version sequence
        /// </summary>
        public uint Sequence
        {
            get { return _sequence; }
            set { SetProperty(ref _sequence, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private uint _sequence;

        /// <summary>
        /// Signature timestamp
        /// </summary>
        public DateTime? SignedAt
        {
            get { return _signed_at; }
            set { SetProperty(ref _signed_at, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DateTime? _signed_at;

        #endregion

        #region Methods

        /// <summary>
        /// Returns appropriate authenticating instance for given connecting instance
        /// </summary>
        /// <param name="connecting_instance">Connecting instance</param>
        /// <returns></returns>
        public virtual Instance GetAuthenticatingInstance(Instance connecting_instance)
        {
            return connecting_instance;
        }

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

        /// <summary>
        /// Loads instance source settings
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        /// <param name="settings">Settings</param>
        public virtual void FromSettings(ConnectWizard wizard, Xml.InstanceSourceSettingsBase settings)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Generates persistable instance source settings
        /// </summary>
        /// <returns>Persistable instance source settings</returns>
        public virtual Xml.InstanceSourceSettingsBase ToSettings()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Select connecting instance by URI
        /// </summary>
        /// <remarks>When the instance with given URI is no longer available, this method returns the most popular instance from the <see cref="ConnectingInstanceList"/> list.</remarks>
        /// <param name="uri">Preferred instance URI</param>
        /// <returns>Connecting instance</returns>
        public Instance SelectConnectingInstance(Uri uri)
        {
            if (uri == null)
                return null;

            // Find the connecting instance by URI.
            var instance = ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == uri.AbsoluteUri);
            if (instance != null)
                return instance;

            // The last connecting instance is no longer available. Select the most popular one among the remaining ones.
            return ConnectingInstanceList.Count > 0 ? ConnectingInstanceList.Aggregate((most_popular_instance, inst) => (most_popular_instance == null || inst.Popularity > most_popular_instance.Popularity ? inst : most_popular_instance)) : null;
        }

        /// <summary>
        /// Removes given instance from history
        /// </summary>
        /// <param name="instance">Instance</param>
        public virtual void ForgetInstance(Instance instance)
        {
            // Remove the instance from history.
            ConnectingInstanceList.Remove(instance);

            // Reset connecting instance.
            if (ConnectingInstance != null && ConnectingInstance.Equals(instance))
                ConnectingInstance = ConnectingInstanceList.FirstOrDefault();
        }

        /// <summary>
        /// Removes entire instance source history
        /// </summary>
        public virtual void Forget()
        {
            ConnectingInstance = null;
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
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());
        }

        #endregion
    }
}

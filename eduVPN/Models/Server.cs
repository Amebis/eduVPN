/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN server
    /// </summary>
    public class Server : BindableBase, INamedEntity, IComparable
    {
        #region Properties

        /// <summary>
        /// Server type
        /// </summary>
        public virtual ServerType ServerType { get => ServerType.Own; }

        /// <summary>
        /// Server/organization identifier as used by eduvpn-common
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Localized display names
        /// </summary>
        public Dictionary<string, string> LocalizedDisplayNames { get; }

        /// <summary>
        /// Profiles that this server has defined
        /// </summary>
        public ProfileDictionary Profiles
        {
            get => _Profiles;
            set => SetProperty(ref _Profiles, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ProfileDictionary _Profiles;

        /// <summary>
        /// List of support contact URLs
        /// </summary>
        public ObservableCollectionEx<Uri> SupportContacts { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a custom server
        /// </summary>
        /// <param name="id">Server/organization identifier as used by eduvpn-common</param>
        public Server(string id)
        {
            Id = id;
            LocalizedDisplayNames = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            SupportContacts = new ObservableCollectionEx<Uri>();
        }

        /// <summary>
        /// Constructs a server
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>identifier</c>, <c>display_name</c>, <c>profiles</c> elements.</param>
        public Server(IReadOnlyDictionary<string, object> obj) :
            this(
                eduJSON.Parser.GetValue(obj, "base_url", out string baseUrl) && baseUrl != null ? new Uri(baseUrl).AbsoluteUri :
                eduJSON.Parser.GetValue(obj, "identifier", out string identifier) && identifier != null ? identifier :
                throw new eduJSON.MissingParameterException("identifier/base_url"))
        {
            eduJSON.Parser.GetDictionary(obj, "display_name", LocalizedDisplayNames);
            if (eduJSON.Parser.GetValue(obj, "profiles", out Dictionary<string, object> profiles) && profiles != null)
                _Profiles = new ProfileDictionary(profiles);
            if (eduJSON.Parser.GetValue(obj, "support_contacts", out List<object> supportContacts) && supportContacts != null)
                foreach (var c in supportContacts)
                    if (c is string cStr && Uri.TryCreate(cStr, UriKind.Absolute, out var cUri))
                        SupportContacts.Add(cUri);
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return
                LocalizedDisplayNames.Count > 0 ? LocalizedDisplayNames.GetLocalized() :
                Uri.TryCreate(Id, UriKind.Absolute, out var uri) ? uri.Host :
                Id;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as Server;
            if (!Id.Equals(other.Id))
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Removes server data from cache
        /// </summary>
        public void Forget()
        {
            lock (Properties.Settings.Default.AccessTokenCache2)
                Properties.Settings.Default.AccessTokenCache2.Remove(Id);
        }

        /// <summary>
        /// Creates server from eduvpn-common JSON string
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>identifier</c>, <c>display_name</c>, <c>profiles</c> elements.</param>
        /// <returns></returns>
        /// <exception cref="eduJSON.ParameterException">Unknown server type</exception>
        public static Server Load(IReadOnlyDictionary<string, object> obj)
        {
            switch ((ServerType)eduJSON.Parser.GetValue<long>(obj, "server_type"))
            {
                case ServerType.InstituteAccess:
                    return new InstituteAccessServer(eduJSON.Parser.GetValue<Dictionary<string, object>>(obj, "institute_access_server"));
                case ServerType.SecureInternet:
                    return new SecureInternetServer(eduJSON.Parser.GetValue<Dictionary<string, object>>(obj, "secure_internet_server"));
                case ServerType.Own:
                    return new Server(eduJSON.Parser.GetValue<Dictionary<string, object>>(obj, "custom_server"));
                default:
                    throw new eduJSON.ParameterException("Unknown server type", "server_type");
            }
        }

        #endregion

        #region IComparable Support

        /// <inheritdoc/>
        public int CompareTo(object obj)
        {
            return ToString().CompareTo(obj.ToString());
        }

        #endregion
    }
}

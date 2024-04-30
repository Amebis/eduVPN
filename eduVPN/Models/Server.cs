/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
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

        #endregion

        #region IComparable Support

        /// <inheritdoc/>
        public int CompareTo(object obj)
        {
            return ToString().CompareTo(obj.ToString());
        }

        #endregion

        #region Utf8Json

        public class Json
        {
            public string server_type { get; set; }
            public Uri base_url { get; set; }
            public string identifier { get; set; }
            public object display_name { get; set; }
            public ProfileDictionary.Json profiles { get; set; }
            public List<Uri> support_contacts { get; set; }
            public bool delisted { get; set; }
            public object keyword_list { get; set; }
            public string country_code { get; set; }
            public List<string> locations { get; set; }
        }

        public class Json2
        {
            public long server_type { get; set; }
            public Json institute_access_server { get; set; }
            public Json secure_internet_server { get; set; }
            public Json custom_server { get; set; }
        }

        public class JsonLists
        {
            public List<Json> institute_access_servers { get; set; }
            public Json secure_internet_server { get; set; }
            public List<Json> custom_servers { get; set; }
        }

        /// <summary>
        /// Constructs a server
        /// </summary>
        /// <param name="json">JSON object</param>
        public Server(Json json) :
            this(json.base_url != null ? json.base_url.AbsoluteUri :
                json.identifier != null ? json.identifier :
                throw new ArgumentException())
        {
            LocalizedDisplayNames = json.display_name.ParseLocalized<string>();
            if (json.profiles != null)
                _Profiles = new ProfileDictionary(json.profiles);
            if (json.support_contacts != null)
                foreach (var c in json.support_contacts)
                    SupportContacts.Add(c);
        }

        /// <summary>
        /// Creates server from eduvpn-common JSON string
        /// </summary>
        /// <param name="json">JSON object</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Unknown server type</exception>
        public static Server Load(Json2 json)
        {
            switch ((ServerType)json.server_type)
            {
                case ServerType.InstituteAccess:
                    return new InstituteAccessServer(json.institute_access_server);
                case ServerType.SecureInternet:
                    return new SecureInternetServer(json.secure_internet_server);
                case ServerType.Own:
                    return new Server(json.custom_server);
                default:
                    throw new ArgumentException("Unknown server type", "server_type");
            }
        }

        #endregion
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;

namespace eduVPN
{
    /// <summary>
    /// An eduVPN API endpoint
    /// </summary>
    public class API : JSON.ILoadableItem
    {
        #region Properties

        /// <summary>
        /// Authorization endpoint URI - used by the client to obtain authorization from the resource owner via user-agent redirection.
        /// </summary>
        public Uri AuthorizationEndpoint { get => _authorization_endpoint; }
        private Uri _authorization_endpoint;

        /// <summary>
        /// Token endpoint URI - used by the client to exchange an authorization grant for an access token, typically with client authentication.
        /// </summary>
        public Uri TokenEndpoint { get => _token_endpoint; }
        private Uri _token_endpoint;

        /// <summary>
        /// API base URI
        /// </summary>
        public Uri BaseURI { get => _base_uri; }
        private Uri _base_uri;

        /// <summary>
        /// Create client certificate URI
        /// </summary>
        public Uri CreateCertificate { get => _create_certificate; }
        private Uri _create_certificate;

        /// <summary>
        /// Profile configuration URI
        /// </summary>
        public Uri ProfileConfig { get => _profile_config; }
        private Uri _profile_config;

        /// <summary>
        /// Profile list URI
        /// </summary>
        public Uri ProfileList { get => _profile_list; }
        private Uri _profile_list;

        /// <summary>
        /// System messages URI
        /// </summary>
        public Uri SystemMessages { get => _system_messages; }
        private Uri _system_messages;

        /// <summary>
        /// User messages URI
        /// </summary>
        public Uri UserMessages { get => _user_messages; }
        private Uri _user_messages;

        #endregion

        #region Methods

        /// <summary>
        /// Loads APIv2 from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>authorization_endpoint</c>, <c>token_endpoint</c> and other optional elements. All elements should be strings representing URI(s).</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public void Load(object obj)
        {
            var obj2 = obj as Dictionary<string, object>;
            if (obj2 == null)
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(Dictionary<string, object>), obj.GetType());

            // Get api object.
            var api = eduJSON.Parser.GetValue<Dictionary<string, object>>(
                eduJSON.Parser.GetValue<Dictionary<string, object>>(obj2, "api"),
                "http://eduvpn.org/api#2");

            // Set authorization endpoint.
            _authorization_endpoint = new Uri(eduJSON.Parser.GetValue<string>(api, "authorization_endpoint"));

            // Set token endpoint.
            _token_endpoint = new Uri(eduJSON.Parser.GetValue<string>(api, "token_endpoint"));

            // Set other URI(s).
            _base_uri = eduJSON.Parser.GetValue(api, "api_base_uri", out string api_base_uri) ?
                new Uri(api_base_uri) :
                null;

            _create_certificate = eduJSON.Parser.GetValue(api, "create_certificate", out string create_certificate) ?
                new Uri(create_certificate) :
                _base_uri != null ? AppendPath(_base_uri, "/create_keypair") : null;

            _profile_config = eduJSON.Parser.GetValue(api, "profile_config", out string profile_config) ?
                new Uri(profile_config) :
                _base_uri != null ? AppendPath(_base_uri, "/profile_config") : null;

            _profile_list = eduJSON.Parser.GetValue(api, "profile_list", out string profile_list) ?
                new Uri(profile_list) :
                _base_uri != null ? AppendPath(_base_uri, "/profile_list") : null;

            _system_messages = eduJSON.Parser.GetValue(api, "system_messages", out string system_messages) ?
                new Uri(system_messages) :
                _base_uri != null ? AppendPath(_base_uri, "/system_messages") : null;

            _user_messages = eduJSON.Parser.GetValue(api, "user_messages", out string user_messages) ?
                new Uri(user_messages) :
                _base_uri != null ? AppendPath(_base_uri, "/user_messages") : null;
        }

        /// <summary>
        /// Appends path to base URI
        /// </summary>
        /// <param name="uri">Base URI</param>
        /// <param name="path">Path to append to</param>
        /// <returns></returns>
        private static Uri AppendPath(Uri uri, string path)
        {
            var uri_builder = new UriBuilder(uri);
            uri_builder.Path += path;
            return uri_builder.Uri;
        }

        #endregion
    }
}

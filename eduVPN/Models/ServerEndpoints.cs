/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN API endpoints
    /// </summary>
    /// <remarks>This class is not bindable by MVVM, as any direct UI interaction is not planned.</remarks>
    public class ServerEndpoints : JSON.ILoadableItem
    {
        #region Properties

        /// <summary>
        /// Authorization endpoint URI - used by the client to obtain authorization from the resource owner via user-agent redirection.
        /// </summary>
        public Uri AuthorizationEndpoint { get; private set; }

        /// <summary>
        /// Token endpoint URI - used by the client to exchange an authorization grant for an access token, typically with client authentication.
        /// </summary>
        public Uri TokenEndpoint { get; private set; }

        /// <summary>
        /// API base URI
        /// </summary>
        public Uri BaseUri { get; private set; }

        /// <summary>
        /// Create client certificate URI
        /// </summary>
        public Uri CreateCertificate { get; private set; }

        /// <summary>
        /// Check client certificate URI
        /// </summary>
        public Uri CheckCertificate { get; private set; }

        /// <summary>
        /// Profile complete OpenVPN configuration URI
        /// </summary>
        public Uri ProfileCompleteConfig { get; private set; }

        /// <summary>
        /// Profile OpenVPN configuration URI
        /// </summary>
        public Uri ProfileConfig { get; private set; }

        /// <summary>
        /// Profile list URI
        /// </summary>
        public Uri Profiles { get; private set; }

        /// <summary>
        /// System messages URI
        /// </summary>
        public Uri SystemMessages { get; private set; }

        /// <summary>
        /// User messages URI
        /// </summary>
        public Uri UserMessages { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes server endpoints
        /// </summary>
        public ServerEndpoints()
        {
        }

        /// <summary>
        /// Initializes server endpoints
        /// </summary>
        /// <param name="authorizationEndpoint">Authorization endpoint URI</param>
        /// <param name="tokenEndpoint">Token endpoint URI</param>
        public ServerEndpoints(Uri authorizationEndpoint, Uri tokenEndpoint)
        {
            AuthorizationEndpoint = authorizationEndpoint;
            TokenEndpoint = tokenEndpoint;
        }

        #endregion

        #region ILoadableItem Support

        /// <summary>
        /// Loads APIv2 from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>authorization_endpoint</c>, <c>token_endpoint</c> and other optional elements. All elements should be strings representing URI(s).</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public void Load(object obj)
        {
            if (!(obj is Dictionary<string, object> obj2))
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());

            // Get api object.
            var api = eduJSON.Parser.GetValue<Dictionary<string, object>>(
                eduJSON.Parser.GetValue<Dictionary<string, object>>(obj2, "api"),
                "http://eduvpn.org/api#2");

            // Set authorization endpoint.
            AuthorizationEndpoint = new Uri(eduJSON.Parser.GetValue<string>(api, "authorization_endpoint"));

            // Set token endpoint.
            TokenEndpoint = new Uri(eduJSON.Parser.GetValue<string>(api, "token_endpoint"));

            // Set other URI(s).
            BaseUri = eduJSON.Parser.GetValue(api, "api_base_uri", out string apiBaseUri) ?
                new Uri(apiBaseUri) :
                null;

            CreateCertificate = eduJSON.Parser.GetValue(api, "create_certificate", out string createCertificate) ?
                new Uri(createCertificate) :
                BaseUri != null ? AppendPath(BaseUri, "/create_keypair") : null;

            CheckCertificate = eduJSON.Parser.GetValue(api, "check_certificate", out string checkCertificate) ?
                new Uri(checkCertificate) :
                BaseUri != null ? AppendPath(BaseUri, "/check_certificate") : null;

            ProfileCompleteConfig = eduJSON.Parser.GetValue(api, "create_config", out string createConfig) ?
                new Uri(createConfig) :
                BaseUri != null ? AppendPath(BaseUri, "/create_config") : null;

            ProfileConfig = eduJSON.Parser.GetValue(api, "profile_config", out string profileConfig) ?
                new Uri(profileConfig) :
                BaseUri != null ? AppendPath(BaseUri, "/profile_config") : null;

            Profiles = eduJSON.Parser.GetValue(api, "profile_list", out string profileList) ?
                new Uri(profileList) :
                BaseUri != null ? AppendPath(BaseUri, "/profile_list") : null;

            SystemMessages = eduJSON.Parser.GetValue(api, "system_messages", out string systemMessages) ?
                new Uri(systemMessages) :
                BaseUri != null ? AppendPath(BaseUri, "/system_messages") : null;

            UserMessages = eduJSON.Parser.GetValue(api, "user_messages", out string userMessages) ?
                new Uri(userMessages) :
                BaseUri != null ? AppendPath(BaseUri, "/user_messages") : null;
        }

        /// <summary>
        /// Appends path to base URI
        /// </summary>
        /// <param name="uri">Base URI</param>
        /// <param name="path">Path to append to</param>
        /// <returns></returns>
        private static Uri AppendPath(Uri uri, string path)
        {
            var uriBuilder = new UriBuilder(uri);
            uriBuilder.Path += path;
            return uriBuilder.Uri;
        }

        #endregion
    }
}

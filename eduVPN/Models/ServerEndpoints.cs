/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
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
        public Uri API { get; private set; }

        /// <summary>
        /// Get "Info" from the VPN server, including a list of available profiles
        /// </summary>
        public Uri Info { get; private set; }

        /// <summary>
        /// "Connect" to a VPN profile
        /// </summary>
        public Uri Connect { get; private set; }

        /// <summary>
        /// "Disconnect" from a VPN profile
        /// </summary>
        public Uri Disconnect { get; private set; }

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
            if (!eduJSON.Parser.GetValue(
                eduJSON.Parser.GetValue<Dictionary<string, object>>(obj2, "api"),
                "http://eduvpn.org/api#3", out Dictionary<string, object> api))
                throw new UnsupportedServerAPIException();

            // Set authorization endpoint.
            AuthorizationEndpoint = new Uri(eduJSON.Parser.GetValue<string>(api, "authorization_endpoint"));

            // Set token endpoint.
            TokenEndpoint = new Uri(eduJSON.Parser.GetValue<string>(api, "token_endpoint"));

            // Set other URI(s).
            API = eduJSON.Parser.GetValue(api, "api_endpoint", out string apiEndpoint) ?
                new Uri(apiEndpoint) :
                null;

            Info = AppendPath(API, "/info");
            Connect = AppendPath(API, "/connect");
            Disconnect = AppendPath(API, "/disconnect");
        }

        /// <summary>
        /// Appends path to base URI
        /// </summary>
        /// <param name="uri">Base URI</param>
        /// <param name="path">Path to append to</param>
        /// <returns>Combined URI</returns>
        private static Uri AppendPath(Uri uri, string path)
        {
            var uriBuilder = new UriBuilder(uri);
            uriBuilder.Path += path;
            return uriBuilder.Uri;
        }

        #endregion
    }
}

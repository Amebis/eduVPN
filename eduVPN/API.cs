/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;

namespace eduVPN
{
    /// <summary>
    /// An eduVPN API endpoint
    /// </summary>
    public class API
    {
        #region Properties

        /// <summary>
        /// Authorization endpoint URI - used by the client to obtain authorization from the resource owner via user-agent redirection.
        /// </summary>
        public Uri AuthorizationEndpoint { get; }

        /// <summary>
        /// Token endpoint URI - used by the client to exchange an authorization grant for an access token, typically with client authentication.
        /// </summary>
        public Uri TokenEndpoint { get; }

        /// <summary>
        /// API base URI
        /// </summary>
        public Uri BaseURI { get; }

        /// <summary>
        /// Create client certificate URI
        /// </summary>
        public Uri CreateCertificate { get; }

        /// <summary>
        /// Profile configuration URI
        /// </summary>
        public Uri ProfileConfig { get; }

        /// <summary>
        /// Profile list URI
        /// </summary>
        public Uri ProfileList { get; }

        /// <summary>
        /// System messages URI
        /// </summary>
        public Uri SystemMessages { get; }

        /// <summary>
        /// User messages URI
        /// </summary>
        public Uri UserMessages { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Loads API URIs from the given instance base URI
        /// </summary>
        /// <param name="uri">Instance URI</param>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public API(Uri uri)
        {
            // Generate API info URI.
            var builder_info = new UriBuilder(uri);
            builder_info.Path += "info.json";

            // Load API data.
            byte[] data;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(builder_info.Uri);
            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.CachePolicy = noCachePolicy;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (BinaryReader reader = new BinaryReader(stream))
                data = reader.ReadBytes(1048576); // Limit to 1MiB

            // Parse the API/APIv2.
            var apiv2 =
                eduJSON.Parser.GetValue< Dictionary<string, object> >(
                    eduJSON.Parser.GetValue< Dictionary<string, object> >(
                        (Dictionary<string, object>)eduJSON.Parser.Parse(Encoding.UTF8.GetString(data)),
                        "api"),
                    "http://eduvpn.org/api#2");

            // Set authorization endpoint.
            AuthorizationEndpoint = new Uri(eduJSON.Parser.GetValue<string>(apiv2, "authorization_endpoint"));

            // Set token endpoint.
            TokenEndpoint = new Uri(eduJSON.Parser.GetValue<string>(apiv2, "token_endpoint"));

            // Set other URI(s).
            if (eduJSON.Parser.GetValue(apiv2, "api_base_uri", out string api_base_uri))
                BaseURI = new Uri(api_base_uri);
            if (eduJSON.Parser.GetValue(apiv2, "create_certificate", out string create_certificate))
                CreateCertificate = new Uri(create_certificate);
            if (eduJSON.Parser.GetValue(apiv2, "profile_config", out string profile_config))
                ProfileConfig = new Uri(profile_config);
            if (eduJSON.Parser.GetValue(apiv2, "profile_list", out string profile_list))
                ProfileList = new Uri(profile_list);
            if (eduJSON.Parser.GetValue(apiv2, "system_messages", out string system_messages))
                SystemMessages = new Uri(system_messages);
            if (eduJSON.Parser.GetValue(apiv2, "user_messages", out string user_messages))
                UserMessages = new Uri(user_messages);
        }

        #endregion
    }
}

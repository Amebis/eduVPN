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

            // Parse data.
            var obj = (Dictionary<string, object>)eduJSON.Parser.Parse(Encoding.UTF8.GetString(data));

            // Parse the API.
            object api;
            if (obj.TryGetValue("api", out api) && api.GetType() == typeof(Dictionary<string, object>))
            {
                // Parse APIv2.
                object apiv2;
                if (((Dictionary<string, object>)api).TryGetValue("http://eduvpn.org/api#2", out apiv2) && apiv2.GetType() == typeof(Dictionary<string, object>))
                {
                    object obj2;

                    // Set authorization endpoint.
                    if (((Dictionary<string, object>)apiv2).TryGetValue("authorization_endpoint", out obj2) && obj2.GetType() == typeof(string))
                        AuthorizationEndpoint = new Uri((string)obj2);
                    else
                        throw new ArgumentException(String.Format(Resources.ErrorMissingDataValue, "authorization_endpoint"), "uri");

                    // Set token endpoint.
                    if (((Dictionary<string, object>)apiv2).TryGetValue("token_endpoint", out obj2) && obj2.GetType() == typeof(string))
                        TokenEndpoint = new Uri((string)obj2);
                    else
                        throw new ArgumentException(String.Format(Resources.ErrorMissingDataValue, "token_endpoint"), "uri");

                    // Set base URI.
                    if (((Dictionary<string, object>)apiv2).TryGetValue("api_base_uri", out obj2) && obj2.GetType() == typeof(string))
                        BaseURI = new Uri((string)obj2);

                    // Set base URI.
                    if (((Dictionary<string, object>)apiv2).TryGetValue("create_certificate", out obj2) && obj2.GetType() == typeof(string))
                        CreateCertificate = new Uri((string)obj2);

                    // Set base URI.
                    if (((Dictionary<string, object>)apiv2).TryGetValue("profile_config", out obj2) && obj2.GetType() == typeof(string))
                        ProfileConfig = new Uri((string)obj2);

                    // Set base URI.
                    if (((Dictionary<string, object>)apiv2).TryGetValue("profile_list", out obj2) && obj2.GetType() == typeof(string))
                        ProfileList = new Uri((string)obj2);

                    // Set base URI.
                    if (((Dictionary<string, object>)apiv2).TryGetValue("system_messages", out obj2) && obj2.GetType() == typeof(string))
                        SystemMessages = new Uri((string)obj2);

                    // Set base URI.
                    if (((Dictionary<string, object>)apiv2).TryGetValue("user_messages", out obj2) && obj2.GetType() == typeof(string))
                        UserMessages = new Uri((string)obj2);
                }
                else
                    throw new ArgumentException(String.Format(Resources.ErrorMissingDataValue, "http://eduvpn.org/api#2"), "uri");
            }
            else
                throw new ArgumentException(String.Format(Resources.ErrorMissingDataValue, "api"), "uri");
        }

        /// <summary>
        /// URI used to obtain an access token
        /// </summary>
        public Uri AuthorizationEndpoint { get; }

        /// <summary>
        /// Token endpoint URI
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
    }
}

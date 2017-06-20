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
using System.Threading;
using System.Threading.Tasks;

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

        #region Constructors

        /// <summary>
        /// Loads APIv2 from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>authorization_endpoint</c>, <c>token_endpoint</c> and other optional elements. All elements should be strings representing URI(s).</param>
        public void Load(Dictionary<string, object> obj)
        {
            // Set authorization endpoint.
            _authorization_endpoint = new Uri(eduJSON.Parser.GetValue<string>(obj, "authorization_endpoint"));

            // Set token endpoint.
            _token_endpoint = new Uri(eduJSON.Parser.GetValue<string>(obj, "token_endpoint"));

            // Set other URI(s).
            _base_uri = eduJSON.Parser.GetValue(obj, "api_base_uri", out string api_base_uri) ? new Uri(api_base_uri) : null;
            _create_certificate = eduJSON.Parser.GetValue(obj, "create_certificate", out string create_certificate) ? new Uri(create_certificate) : null;
            _profile_config = eduJSON.Parser.GetValue(obj, "profile_config", out string profile_config) ? new Uri(profile_config) : null;
            _profile_list = eduJSON.Parser.GetValue(obj, "profile_list", out string profile_list) ? new Uri(profile_list) : null;
            _system_messages = eduJSON.Parser.GetValue(obj, "system_messages", out string system_messages) ? new Uri(system_messages) : null;
            _user_messages = eduJSON.Parser.GetValue(obj, "user_messages", out string user_messages) ? new Uri(user_messages) : null;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets API URIs from the given instance base URI.
        /// </summary>
        /// <param name="uri">Instance URI</param>
        /// <param name="ct">The token to monitor for cancellation requests.</param>
        /// <returns>Dictionary object</returns>
        public static Dictionary<string, object> Get(Uri uri, CancellationToken ct = default(CancellationToken))
        {
            var task = GetAsync(uri, ct);
            try
            {
                task.Wait(ct);
                return task.Result;
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Gets API URIs from the given instance base URI asynchronously.
        /// </summary>
        /// <param name="uri">Instance URI</param>
        /// <param name="ct">The token to monitor for cancellation requests.</param>
        /// <returns>Asynchronous operation with expected dictionary object</returns>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "HttpWebResponse and Stream tolerate multiple disposes.")]
        public static async Task<Dictionary<string, object>> GetAsync(Uri uri, CancellationToken ct = default(CancellationToken))
        {
            // Load API data.
            var data = new byte[1048576]; // Limit to 1MiB
            int data_size;
            var request = (HttpWebRequest)WebRequest.Create(uri);
            var noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.CachePolicy = noCachePolicy;
            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            using (var stream = response.GetResponseStream())
            {
                var read_task = stream.ReadAsync(data, 0, data.Length, ct);
                data_size = await read_task;
                if (read_task.IsCanceled)
                    throw new OperationCanceledException(ct);
            }

            // Parse the API/APIv2.
            return eduJSON.Parser.GetValue<Dictionary<string, object>>(
                eduJSON.Parser.GetValue<Dictionary<string, object>>(
                    (Dictionary<string, object>)eduJSON.Parser.Parse(Encoding.UTF8.GetString(data, 0, data_size), ct),
                    "api"),
                "http://eduvpn.org/api#2");
        }

        #endregion
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
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
    /// A helper class to return JSON response
    /// </summary>
    public class JSONContents
    {
        #region Fields

        private static readonly HttpRequestCachePolicy _no_cache_policy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);

        #endregion

        #region Properties

        /// <summary>
        /// JSON string
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Content timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// <c>true</c> - the content was freshly loaded, <c>false</c> - Content not modified
        /// </summary>
        public bool IsFresh { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Gets JSON from the given URI.
        /// </summary>
        /// <param name="uri">URI</param>
        /// <param name="pub_key">Public key for signature verification; or <c>null</c> if signature verification is not required.</param>
        /// <param name="ct">The token to monitor for cancellation requests.</param>
        /// <param name="previous">Previous JSON content, when refresh is required.</param>
        /// <returns>JSON content</returns>
        public static JSONContents Get(Uri uri, byte[] pub_key = null, CancellationToken ct = default(CancellationToken), JSONContents previous = null)
        {
            var task = GetAsync(uri, pub_key, ct, previous);
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
        /// Gets JSON from the given URI asynchronously.
        /// </summary>
        /// <param name="uri">URI</param>
        /// <param name="pub_key">Public key for signature verification; or <c>null</c> if signature verification is not required.</param>
        /// <param name="ct">The token to monitor for cancellation requests.</param>
        /// <param name="previous">Previous JSON content, when refresh is required.</param>
        /// <returns>Asynchronous operation with expected JSON string</returns>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "HttpWebResponse, Stream, and StreamReader tolerate multiple disposes.")]
        public static async Task<JSONContents> GetAsync(Uri uri, byte[] pub_key = null, CancellationToken ct = default(CancellationToken), JSONContents previous = null)
        {
            // Spawn data loading.
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.CachePolicy = _no_cache_policy;
            request.Accept = "application/json";
            if (previous != null)
                request.IfModifiedSince = previous.Timestamp;
            var response_task = request.GetResponseAsync();

            HttpWebResponse response;
            try
            {
                // Wait for data to start comming in.
                response = (HttpWebResponse)(await response_task);
            }
            catch (WebException ex)
            {
                // When the content was not modified, return the previous one.
                if (ex.Response != null && ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotModified)
                {
                    previous.IsFresh = false;
                    return previous;
                }
                else
                    throw;
            }

            using (response)
            {
                byte[] signature = null;
                Task<WebResponse> response_sig_task = null;
                if (pub_key != null)
                {
                    // Generate signature URI.
                    var builder_sig = new UriBuilder(uri);
                    builder_sig.Path += ".sig";

                    // Spawn signature loading.
                    request = (HttpWebRequest)WebRequest.Create(builder_sig.Uri);
                    request.CachePolicy = _no_cache_policy;
                    request.Accept = "application/pgp-signature";
                    response_sig_task = request.GetResponseAsync();
                }

                var data = new byte[1048576]; // Limit to 1MiB
                int data_size;
                using (var stream = response.GetResponseStream())
                {
                    // Spawn data read.
                    var read_task = stream.ReadAsync(data, 0, data.Length, ct);

                    if (pub_key != null)
                    {
                        // Read the signature.
                        using (var response_sig = (HttpWebResponse)(await response_sig_task))
                        using (var stream_sig = response_sig.GetResponseStream())
                        using (var reader_sig = new StreamReader(stream_sig))
                            signature = Convert.FromBase64String(await reader_sig.ReadToEndAsync());
                    }

                    // Wait for the data to arrive.
                    data_size = await read_task;
                    if (read_task.IsCanceled)
                        throw new OperationCanceledException(ct);

                    if (pub_key != null)
                    {
                        ct.ThrowIfCancellationRequested();

                        // Verify signature.
                        using (eduEd25519.ED25519 key = new eduEd25519.ED25519(pub_key))
                            if (!key.VerifyDetached(data, 0, data_size, signature))
                                throw new System.Security.SecurityException(String.Format(Resources.Strings.ErrorInvalidSignature, uri));
                    }
                }

                return new JSONContents()
                {
                    Value = Encoding.UTF8.GetString(data, 0, data_size),
                    Timestamp = DateTime.TryParse(response.GetResponseHeader("Last-Modified"), out var _timestamp) ? _timestamp : default(DateTime),
                    IsFresh = true
                };
            }
        }

        #endregion
    }
}

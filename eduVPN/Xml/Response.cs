/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Xml
{
    /// <summary>
    /// A helper class to return text response
    /// </summary>
    public class Response : IXmlSerializable
    {
        #region Fields

        /// <summary>
        /// Executing assembly
        /// </summary>
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

        /// <summary>
        /// User agent
        /// </summary>
        public static readonly string UserAgent = (Attribute.GetCustomAttributes(_assembly, typeof(AssemblyTitleAttribute)).SingleOrDefault() as AssemblyTitleAttribute)?.Title + "/" + _assembly?.GetName()?.Version?.ToString();

        /// <summary>
        /// Caching policy
        /// </summary>
        public static readonly HttpRequestCachePolicy CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);

        #endregion

        #region Properties

        /// <summary>
        /// String content (JSON, plain text, etc.)
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Content timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Content ETag
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// <c>true</c> - the content was freshly loaded, <c>false</c> - Content not modified
        /// </summary>
        public bool IsFresh { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Gets UTF-8 text from the given URI.
        /// </summary>
        /// <param name="uri">URI</param>
        /// <param name="param">Parameters to be sent as <c>application/x-www-form-urlencoded</c> name-value pairs</param>
        /// <param name="token">OAuth access token</param>
        /// <param name="response_type">Expected response MIME type</param>
        /// <param name="pub_key">Public key for signature verification; or <c>null</c> if signature verification is not required</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <param name="previous">Previous content, when refresh is required</param>
        /// <returns>Content</returns>
        public static Response Get(Uri uri, NameValueCollection param = null, AccessToken token = null, string response_type = "application/json", byte[] pub_key = null, CancellationToken ct = default(CancellationToken), Response previous = null)
        {
            var task = GetAsync(uri, param, token, response_type, pub_key, ct, previous);
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
        /// Gets UTF-8 text from the given URI asynchronously.
        /// </summary>
        /// <param name="uri">URI</param>
        /// <param name="param">Parameters to be sent as <c>application/x-www-form-urlencoded</c> name-value pairs</param>
        /// <param name="token">OAuth access token</param>
        /// <param name="response_type">Expected response MIME type</param>
        /// <param name="pub_key">Public key for signature verification; or <c>null</c> if signature verification is not required</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <param name="previous">Previous content, when refresh is required</param>
        /// <returns>Asynchronous operation with expected content</returns>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "HttpWebResponse, Stream, and StreamReader tolerate multiple disposes.")]
        public static async Task<Response> GetAsync(Uri uri, NameValueCollection param = null, AccessToken token = null, string response_type = "application/json", byte[] pub_key = null, CancellationToken ct = default(CancellationToken), Response previous = null)
        {
            // Spawn data loading.
            var request = WebRequest.Create(uri);
            request.CachePolicy = CachePolicy;
            if (token != null)
                token.AddToRequest(request);
            if (request is HttpWebRequest request_web)
            {
                request_web.UserAgent = UserAgent;
                request_web.Accept = response_type;
                if (previous != null && param != null)
                {
                    request_web.IfModifiedSince = previous.Timestamp;

                    if (previous.ETag != null)
                        request_web.Headers.Add("If-None-Match", previous.ETag);
                }
            }

            if (param != null)
            {
                // Send data.
                UTF8Encoding utf8 = new UTF8Encoding();
                var body_binary = Encoding.ASCII.GetBytes(string.Join("&", param.Cast<string>().Select(e => string.Format("{0}={1}", HttpUtility.UrlEncode(e, utf8), HttpUtility.UrlEncode(param[e], utf8)))));
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = body_binary.Length;
                try
                {
                    using (var stream_req = await request.GetRequestStreamAsync())
                        await stream_req.WriteAsync(body_binary, 0, body_binary.Length, ct);
                }
                catch (WebException ex) { throw new AggregateException(Resources.Strings.ErrorUploading, ex); }
            }

            WebResponse response;
            try
            {
                // Wait for data to start comming in.
                response = await request.GetResponseAsync();
            }
            catch (WebException ex)
            {
                // When the content was not modified, return the previous one.
                if (ex.Response != null && ex.Response is HttpWebResponse response_http && response_http.StatusCode == HttpStatusCode.NotModified)
                {
                    previous.IsFresh = false;
                    return previous;
                }
                else
                    throw new AggregateException(Resources.Strings.ErrorDownloading, ex);
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
                    request = WebRequest.Create(builder_sig.Uri);
                    request.CachePolicy = CachePolicy;
                    if (request is HttpWebRequest request_web_sig)
                    {
                        request_web_sig.UserAgent = UserAgent;
                        request_web_sig.Accept = "application/pgp-signature";
                    }

                    response_sig_task = request.GetResponseAsync();
                }

                var data = new byte[0];
                using (var stream = response.GetResponseStream())
                {
                    // Spawn data chunk read.
                    var buffer = new byte[1048576];
                    var read_task = stream.ReadAsync(buffer, 0, buffer.Length, ct);

                    if (pub_key != null)
                    {
                        // Read the signature.
                        using (var response_sig = await response_sig_task)
                        using (var stream_sig = response_sig.GetResponseStream())
                        using (var reader_sig = new StreamReader(stream_sig))
                            signature = Convert.FromBase64String(await reader_sig.ReadToEndAsync());
                    }

                    for (; ; )
                    {
                        // Wait for the data to arrive.
                        await read_task;
                        if (read_task.IsCanceled)
                            throw new OperationCanceledException(ct);
                        if (read_task.Result == 0)
                            break;

                        // Append it to the data.
                        var data_new = new byte[data.LongLength + read_task.Result];
                        Array.Copy(data, data_new, data.LongLength);
                        Array.Copy(buffer, 0, data_new, data.LongLength, read_task.Result);
                        data = data_new;

                        // Respawn data chunk read.
                        read_task = stream.ReadAsync(buffer, 0, buffer.Length, ct);
                    }

                    if (pub_key != null)
                    {
                        ct.ThrowIfCancellationRequested();

                        // Verify signature.
                        using (eduEd25519.ED25519 key = new eduEd25519.ED25519(pub_key))
                            if (!key.VerifyDetached(data, signature))
                                throw new System.Security.SecurityException(String.Format(Resources.Strings.ErrorInvalidSignature, uri));
                    }
                }

                return
                    response is HttpWebResponse response_web ?
                    new Response()
                    {
                        Value = Encoding.UTF8.GetString(data),
                        Timestamp = DateTime.TryParse(response_web.GetResponseHeader("Last-Modified"), out var _timestamp) ? _timestamp : default(DateTime),
                        ETag = response_web.GetResponseHeader("ETag"),
                        IsFresh = true
                    } :
                    new Response()
                    {
                        Value = Encoding.UTF8.GetString(data),
                        IsFresh = true
                    };
            }
        }

        #endregion

        #region IXmlSerializable Support

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            string v;

            Value = reader[nameof(Value)];
            Timestamp = DateTime.TryParse(reader[nameof(Timestamp)], out var timestamp) ? timestamp : default(DateTime);
            ETag = reader[nameof(ETag)];
            IsFresh = (v = reader[nameof(IsFresh)]) != null && bool.TryParse(v, out var v_is_fresh) ? v_is_fresh : false;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(nameof(Value), Value);
            writer.WriteAttributeString(nameof(Timestamp), Timestamp.ToString("o"));
            writer.WriteAttributeString(nameof(ETag), ETag);
            writer.WriteAttributeString(nameof(IsFresh), IsFresh.ToString());
        }

        #endregion
    }
}

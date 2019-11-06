/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Text;
using System.Threading;
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
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "HttpWebResponse, Stream, and StreamReader tolerate multiple disposes.")]
        public static Response Get(Uri uri, NameValueCollection param = null, AccessToken token = null, string response_type = "application/json", byte[] pub_key = null, CancellationToken ct = default(CancellationToken), Response previous = null)
        {
            // Create request.
            var request = WebRequest.Create(uri);
            request.CachePolicy = CachePolicy;
            request.Proxy = null;
            if (token != null)
                token.AddToRequest(request);
            if (request is HttpWebRequest request_http)
            {
                request_http.UserAgent = UserAgent;
                request_http.Accept = response_type;
                if (previous != null && param != null)
                {
                    request_http.IfModifiedSince = previous.Timestamp;

                    if (previous.ETag != null)
                        request_http.Headers.Add("If-None-Match", previous.ETag);
                }
            }

            if (param != null)
            {
                // Send data.
                UTF8Encoding utf8 = new UTF8Encoding();
                var body_binary = Encoding.ASCII.GetBytes(String.Join("&", param.Cast<string>().Select(e => String.Format("{0}={1}", HttpUtility.UrlEncode(e, utf8), HttpUtility.UrlEncode(param[e], utf8)))));
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = body_binary.Length;
                try
                {
                    using (var stream_req = request.GetRequestStream())
                    {
                        var task = stream_req.WriteAsync(body_binary, 0, body_binary.Length, ct);
                        try { task.Wait(ct); }
                        catch (AggregateException ex) { throw ex.InnerException; }
                    }
                }
                catch (WebException ex) { throw new AggregateException(Resources.Strings.ErrorUploading, ex.Response is HttpWebResponse ? new WebExceptionEx(ex, ct) : ex); }
            }

            ct.ThrowIfCancellationRequested();

            // Wait for data to start comming in.
            WebResponse response;
            try { response = request.GetResponse(); }
            catch (WebException ex)
            {
                // When the content was not modified, return the previous one.
                if (ex.Response is HttpWebResponse response_http)
                {
                    if (response_http.StatusCode == HttpStatusCode.NotModified)
                    {
                        previous.IsFresh = false;
                        return previous;
                    }

                    throw new WebExceptionEx(ex, ct);
                }

                throw new AggregateException(Resources.Strings.ErrorDownloading, ex);
            }

            ct.ThrowIfCancellationRequested();

            using (response)
            {
                // Read the data.
                var data = new byte[0];
                using (var stream = response.GetResponseStream())
                {
                    var buffer = new byte[1048576];
                    for (; ; )
                    {
                        // Read data chunk.
                        var task = stream.ReadAsync(buffer, 0, buffer.Length, ct);
                        try { task.Wait(ct); }
                        catch (AggregateException ex) { throw ex.InnerException; }
                        if (task.Result == 0)
                            break;

                        // Append it to the data.
                        var data_new = new byte[data.LongLength + task.Result];
                        Array.Copy(data, data_new, data.LongLength);
                        Array.Copy(buffer, 0, data_new, data.LongLength, task.Result);
                        data = data_new;
                    }
                }

                if (pub_key != null)
                {
                    // Generate signature URI.
                    var builder_sig = new UriBuilder(uri);
                    builder_sig.Path += ".sig";

                    // Create signature request.
                    request = WebRequest.Create(builder_sig.Uri);
                    request.CachePolicy = CachePolicy;
                    request.Proxy = null;
                    if (token != null)
                        token.AddToRequest(request);
                    if (request is HttpWebRequest request_http_sig)
                    {
                        request_http_sig.UserAgent = UserAgent;
                        request_http_sig.Accept = "application/pgp-signature";
                    }

                    // Read the signature.
                    byte[] signature = null;
                    using (var response_sig = request.GetResponse())
                    using (var stream_sig = response_sig.GetResponseStream())
                    {
                        ct.ThrowIfCancellationRequested();

                        using (var reader_sig = new StreamReader(stream_sig))
                        {
                            var task = reader_sig.ReadToEndAsync();
                            try { task.Wait(ct); }
                            catch (AggregateException ex) { throw ex.InnerException; }
                            signature = Convert.FromBase64String(task.Result);
                        }
                    }

                    ct.ThrowIfCancellationRequested();

                    // Verify signature.
                    using (eduEd25519.ED25519 key = new eduEd25519.ED25519(pub_key))
                        if (!key.VerifyDetached(data, signature))
                            throw new System.Security.SecurityException(String.Format(Resources.Strings.ErrorInvalidSignature, uri));
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

        /// <summary>
        /// This method is reserved and should not be used.
        /// </summary>
        /// <returns><c>null</c></returns>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="XmlReader"/> stream from which the object is deserialized.</param>
        public void ReadXml(XmlReader reader)
        {
            string v;

            Value = reader[nameof(Value)];
            Timestamp = DateTime.TryParse(reader[nameof(Timestamp)], out var timestamp) ? timestamp : default(DateTime);
            ETag = reader[nameof(ETag)];
            IsFresh = (v = reader[nameof(IsFresh)]) != null && bool.TryParse(v, out var v_is_fresh) ? v_is_fresh : false;
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> stream to which the object is serialized.</param>
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

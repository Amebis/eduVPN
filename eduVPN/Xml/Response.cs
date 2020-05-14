/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduEx.Async;
using eduOAuth;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Security;
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
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <param name="previous">Previous content, when refresh is required</param>
        /// <returns>Content</returns>
        public static Response Get(Uri uri, NameValueCollection param = null, AccessToken token = null, string response_type = "application/json", CancellationToken ct = default(CancellationToken), Response previous = null)
        {
            return Get(new ResourceRef() { Uri = uri }, param, token, response_type, ct, previous);
        }

        /// <summary>
        /// Gets UTF-8 text from the given URI.
        /// </summary>
        /// <param name="res">URI and public key for signature verification</param>
        /// <param name="param">Parameters to be sent as <c>application/x-www-form-urlencoded</c> name-value pairs</param>
        /// <param name="token">OAuth access token</param>
        /// <param name="response_type">Expected response MIME type</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <param name="previous">Previous content, when refresh is required</param>
        /// <returns>Content</returns>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "HttpWebResponse, Stream, and StreamReader tolerate multiple disposes.")]
        public static Response Get(ResourceRef res, NameValueCollection param = null, AccessToken token = null, string response_type = "application/json", CancellationToken ct = default(CancellationToken), Response previous = null)
        {
            // Create request.
            var request = WebRequest.Create(res.Uri);
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
                        stream_req.Write(body_binary, 0, body_binary.Length, ct);
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
                        var count = stream.Read(buffer, 0, buffer.Length, ct);
                        if (count == 0)
                            break;

                        // Append it to the data.
                        var data_new = new byte[data.LongLength + count];
                        Array.Copy(data, data_new, data.LongLength);
                        Array.Copy(buffer, 0, data_new, data.LongLength, count);
                        data = data_new;
                    }
                }

                // Try all supported signature schemes.
                // signature_validation_exceptions will be reset to null on first successful signature validation.
                var signature_validation_exceptions = new Collection<Exception>();
                bool must_check_signature = false;

                if (signature_validation_exceptions != null && res.PublicKey != null)
                {
                    must_check_signature = true;
                    try
                    {
                        // Generate signature URI.
                        var builder_sig = new UriBuilder(res.Uri);
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
                            request_http_sig.Accept = "text/plain";
                        }

                        // Read the ED25519 signature.
                        byte[] signature = null;
                        try
                        {
                            using (var response_sig = request.GetResponse())
                            using (var stream_sig = response_sig.GetResponseStream())
                            {
                                ct.ThrowIfCancellationRequested();

                                using (var reader_sig = new StreamReader(stream_sig))
                                    signature = Convert.FromBase64String(reader_sig.ReadToEnd(ct));
                            }
                        }
                        catch (WebException ex) { throw new AggregateException(Resources.Strings.ErrorDownloadingSignature, ex.Response is HttpWebResponse ? new WebExceptionEx(ex, ct) : ex); }

                        ct.ThrowIfCancellationRequested();

                        // Verify ED25519 signature.
                        using (eduEd25519.ED25519 key = new eduEd25519.ED25519(res.PublicKey))
                            if (!key.VerifyDetached(data, signature))
                                throw new SecurityException(String.Format(Resources.Strings.ErrorInvalidSignature, res.Uri));
                        signature_validation_exceptions = null;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) { signature_validation_exceptions.Add(ex); }
                }

                if (signature_validation_exceptions != null && res.MinisignPublicKeys != null)
                {
                    must_check_signature = true;
                    try
                    {
                        // Generate signature URI.
                        var builder_sig = new UriBuilder(res.Uri);
                        builder_sig.Path += ".minisig";

                        // Create signature request.
                        request = WebRequest.Create(builder_sig.Uri);
                        request.CachePolicy = CachePolicy;
                        request.Proxy = null;
                        if (token != null)
                            token.AddToRequest(request);
                        if (request is HttpWebRequest request_http_sig)
                        {
                            request_http_sig.UserAgent = UserAgent;
                            request_http_sig.Accept = "text/plain";
                        }

                        // Read the Minisign signature.
                        byte[] signature = null;
                        try
                        {
                            using (var response_sig = request.GetResponse())
                            using (var stream_sig = response_sig.GetResponseStream())
                            {
                                ct.ThrowIfCancellationRequested();

                                using (var reader_sig = new StreamReader(stream_sig))
                                {
                                    foreach (var l in reader_sig.ReadToEnd(ct).Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                                    {
                                        if (l.Trim().StartsWith($"untrusted comment:"))
                                            continue;
                                        signature = Convert.FromBase64String(l);
                                        break;
                                    }
                                    if (signature == null)
                                        throw new SecurityException(String.Format(Resources.Strings.ErrorInvalidSignature, res.Uri));
                                }
                            }
                        }
                        catch (WebException ex) { throw new AggregateException(Resources.Strings.ErrorDownloadingSignature, ex.Response is HttpWebResponse ? new WebExceptionEx(ex, ct) : ex); }

                        ct.ThrowIfCancellationRequested();

                        // Verify Minisign signature.
                        using (var s = new MemoryStream(signature, false))
                        using (var r = new BinaryReader(s))
                        {
                            if (r.ReadChar() != 'E')
                                throw new ArgumentException(Resources.Strings.ErrorUnsupportedMinisignSignature);
                            byte[] payload;
                            switch (r.ReadChar())
                            {
                                case 'd': // PureEdDSA
                                    payload = data;
                                    break; 

                                case 'D': // HashedEdDSA
                                    payload = new eduEd25519.BLAKE2b(512).ComputeHash(data);
                                    break;

                                default:
                                    throw new ArgumentException(Resources.Strings.ErrorUnsupportedMinisignSignature);
                            }
                            ulong key_id = r.ReadUInt64();
                            if (!res.MinisignPublicKeys.ContainsKey(key_id))
                                throw new SecurityException(Resources.Strings.ErrorUntrustedMinisignSignatureKey);
                            var sig = new byte[64];
                            if (r.Read(sig, 0, 64) != 64)
                                throw new ArgumentException(Resources.Strings.ErrorInvalidMinisignSignature);
                            using (eduEd25519.ED25519 key = new eduEd25519.ED25519(res.MinisignPublicKeys[key_id]))
                                if (!key.VerifyDetached(payload, sig))
                                    throw new SecurityException(String.Format(Resources.Strings.ErrorInvalidSignature, res.Uri));
                            signature_validation_exceptions = null;
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) { signature_validation_exceptions.Add(ex); }
                }

                if (must_check_signature && signature_validation_exceptions != null)
                    throw new AggregateException(signature_validation_exceptions);

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

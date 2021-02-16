/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduEx.Async;
using eduOAuth;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
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
        private static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();

        /// <summary>
        /// User agent
        /// </summary>
        public static readonly string UserAgent = (Attribute.GetCustomAttributes(ExecutingAssembly, typeof(AssemblyTitleAttribute)).SingleOrDefault() as AssemblyTitleAttribute)?.Title + "/" + ExecutingAssembly?.GetName()?.Version?.ToString();

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
        /// <param name="responseType">Expected response MIME type</param>
        /// <param name="previous">Previous content, when refresh is required</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Content</returns>
        public static Response Get(Uri uri, NameValueCollection param = null, AccessToken token = null, string responseType = "application/json", Response previous = null, CancellationToken ct = default)
        {
            return Get(new ResourceRef() { Uri = uri }, param, token, responseType, previous, ct);
        }

        /// <summary>
        /// Gets UTF-8 text from the given URI.
        /// </summary>
        /// <param name="res">URI and public key for signature verification</param>
        /// <param name="param">Parameters to be sent as <c>application/x-www-form-urlencoded</c> name-value pairs</param>
        /// <param name="token">OAuth access token</param>
        /// <param name="responseType">Expected response MIME type</param>
        /// <param name="previous">Previous content, when refresh is required</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Content</returns>
        public static Response Get(ResourceRef res, NameValueCollection param = null, AccessToken token = null, string responseType = "application/json", Response previous = null, CancellationToken ct = default)
        {
            // Create request.
            var request = WebRequest.Create(res.Uri);
            request.CachePolicy = CachePolicy;
            request.Proxy = null;
            if (token != null)
                token.AddToRequest(request);
            if (request is HttpWebRequest httpRequest)
            {
                httpRequest.UserAgent = UserAgent;
                httpRequest.Accept = responseType;
                if (previous != null && param != null)
                {
                    httpRequest.IfModifiedSince = previous.Timestamp;

                    if (previous.ETag != null)
                        httpRequest.Headers.Add("If-None-Match", previous.ETag);
                }
            }

            if (param != null)
            {
                // Send data.
                UTF8Encoding utf8 = new UTF8Encoding();
                var binBody = Encoding.ASCII.GetBytes(string.Join("&", param.Cast<string>().Select(e => string.Format("{0}={1}", HttpUtility.UrlEncode(e, utf8), HttpUtility.UrlEncode(param[e], utf8)))));
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = binBody.Length;
                try
                {
                    using (var requestStream = request.GetRequestStream())
                        requestStream.Write(binBody, 0, binBody.Length, ct);
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
                if (ex.Response is HttpWebResponse httpResponse)
                {
                    if (httpResponse.StatusCode == HttpStatusCode.NotModified)
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
                        var newData = new byte[data.LongLength + count];
                        Array.Copy(data, newData, data.LongLength);
                        Array.Copy(buffer, 0, newData, data.LongLength, count);
                        data = newData;
                    }
                }

                if (res.PublicKeys != null)
                {
                    // Generate signature URI.
                    var uriBuilderSig = new UriBuilder(res.Uri);
                    uriBuilderSig.Path += ".minisig";

                    // Create signature request.
                    request = WebRequest.Create(uriBuilderSig.Uri);
                    request.CachePolicy = CachePolicy;
                    request.Proxy = null;
                    if (token != null)
                        token.AddToRequest(request);
                    if (request is HttpWebRequest httpRequestSig)
                    {
                        httpRequestSig.UserAgent = UserAgent;
                        httpRequestSig.Accept = "text/plain";
                    }

                    // Read the Minisign signature.
                    byte[] signature = null;
                    try
                    {
                        using (var responseSig = request.GetResponse())
                        using (var streamSig = responseSig.GetResponseStream())
                        {
                            ct.ThrowIfCancellationRequested();

                            using (var readerSig = new StreamReader(streamSig))
                            {
                                foreach (var l in readerSig.ReadToEnd(ct).Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    if (l.Trim().StartsWith($"untrusted comment:"))
                                        continue;
                                    signature = Convert.FromBase64String(l);
                                    break;
                                }
                                if (signature == null)
                                    throw new SecurityException(string.Format(Resources.Strings.ErrorInvalidSignature, res.Uri));
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
                        ulong keyId = r.ReadUInt64();
                        if (!res.PublicKeys.ContainsKey(keyId))
                            throw new SecurityException(Resources.Strings.ErrorUntrustedMinisignPublicKey);
                        var sig = new byte[64];
                        if (r.Read(sig, 0, 64) != 64)
                            throw new ArgumentException(Resources.Strings.ErrorInvalidMinisignSignature);
                        using (eduEd25519.ED25519 key = new eduEd25519.ED25519(res.PublicKeys[keyId]))
                            if (!key.VerifyDetached(payload, sig))
                                throw new SecurityException(string.Format(Resources.Strings.ErrorInvalidSignature, res.Uri));
                    }
                }

                return
                    response is HttpWebResponse webResponse ?
                    new Response()
                    {
                        Value = Encoding.UTF8.GetString(data),
                        Timestamp = DateTime.TryParse(webResponse.GetResponseHeader("Last-Modified"), out var timestamp) ? timestamp : default,
                        ETag = webResponse.GetResponseHeader("ETag"),
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
            Timestamp = DateTime.TryParse(reader[nameof(Timestamp)], out var timestamp) ? timestamp : default;
            ETag = reader[nameof(ETag)];
            IsFresh = (v = reader[nameof(IsFresh)]) != null && bool.TryParse(v, out var isFresh) && isFresh;
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

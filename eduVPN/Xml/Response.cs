/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduEx.Async;
using eduEx.System;
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
        private static readonly string UserAgent = (Attribute.GetCustomAttributes(ExecutingAssembly, typeof(AssemblyTitleAttribute)).SingleOrDefault() as AssemblyTitleAttribute)?.Title + "/" + ExecutingAssembly?.GetName()?.Version?.ToString();

        /// <summary>
        /// Caching policy
        /// </summary>
        private static readonly HttpRequestCachePolicy CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);

        #endregion

        #region Properties

        /// <summary>
        /// String content (JSON, plain text, etc.)
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Content MIME type
        /// </summary>
        public string ContentType { get; private set; }

        /// <summary>
        /// Content date
        /// </summary>
        public DateTimeOffset Date { get; private set; }

        /// <summary>
        /// Content expiration
        /// </summary>
        public DateTimeOffset Expires { get; private set; }

        /// <summary>
        /// Content last modification time
        /// </summary>
        public DateTimeOffset LastModified { get; private set; }

        /// <summary>
        /// Content ETag
        /// </summary>
        public string ETag { get; private set; }

        /// <summary>
        /// Date when token used to obtain this request was authorized
        /// </summary>
        public DateTimeOffset Authorized { get; private set; }

        /// <summary>
        /// <c>true</c> - the content was freshly loaded, <c>false</c> - Content not modified
        /// </summary>
        public bool IsFresh { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a standardized web request
        /// </summary>
        /// <param name="uri">Request URI</param>
        /// <param name="token">Acces token</param>
        /// <param name="responseType">Expected response MIME type</param>
        /// <returns>WebRequest object</returns>
        public static WebRequest CreateRequest(Uri uri, AccessToken token = null, string responseType = "*/*")
        {
            var request = WebRequest.Create(uri);
            request.CachePolicy = CachePolicy;
            request.Proxy = null;
            if (token != null)
                token.AddToRequest(request);
            if (request is HttpWebRequest httpRequest)
            {
                httpRequest.Accept = responseType;
                httpRequest.UserAgent = UserAgent;

                // Do HTTP redirection manually to refuse non-https schemes as per APIv3 requirement.
                // Note: By turning AllowAutoRedirect to false, request.GetResponse() will no longer throw on 3xx status.
                httpRequest.AllowAutoRedirect = false;
            }
            return request;
        }

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
            return Get(new ResourceRef(uri), param, token, responseType, previous, ct);
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
            WebRequest request;
            WebResponse response;
            var uri = res.Uri;

            for (var redirectHop = 0; ; redirectHop++)
            {
                ct.ThrowIfCancellationRequested();

                // Create request.
                request = CreateRequest(uri, token, responseType);
                if (request is HttpWebRequest httpRequest)
                {
                    if (previous != null && param == null)
                    {
                        if (previous.LastModified != DateTimeOffset.MinValue)
                            httpRequest.IfModifiedSince = previous.LastModified.UtcDateTime;

                        if (previous.ETag != null)
                            httpRequest.Headers.Add("If-None-Match", previous.ETag);
                    }
                }

                if (param != null)
                {
                    // Send data.
                    var utf8 = new UTF8Encoding();
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
                try
                {
                    response = request.GetResponse();
                    if (response is HttpWebResponse httpResponse)
                    {
                        switch (httpResponse.StatusCode)
                        {
                            case HttpStatusCode.OK:
                            case HttpStatusCode.Created:
                            case HttpStatusCode.Accepted:
                            case HttpStatusCode.NonAuthoritativeInformation:
                            case HttpStatusCode.NoContent:
                            case HttpStatusCode.ResetContent:
                            case HttpStatusCode.PartialContent:
                                break;

                            case HttpStatusCode.MovedPermanently:
                            case HttpStatusCode.TemporaryRedirect:
                            case (HttpStatusCode)308:
                                // Redirect using the same method.
                                if (redirectHop >= (request as HttpWebRequest).MaximumAutomaticRedirections)
                                    throw new HttpTooMayRedirectsException();
                                uri = new Uri(uri, httpResponse.GetResponseHeader("Location"));
                                if (uri.Scheme != "https")
                                    throw new HttpRedirectToUnsafeUriException();
                                continue;

                            case HttpStatusCode.Found:
                            case HttpStatusCode.SeeOther:
                                // Redirect using GET method.
                                if (redirectHop >= (request as HttpWebRequest).MaximumAutomaticRedirections)
                                    throw new HttpTooMayRedirectsException();
                                uri = new Uri(uri, httpResponse.GetResponseHeader("Location"));
                                if (uri.Scheme != "https")
                                    throw new HttpRedirectToUnsafeUriException();
                                param = null;
                                continue;

                            case HttpStatusCode.NotModified:
                                // When the content was not modified, return the previous one.
                                previous.IsFresh = false;
                                return previous;

                            default:
                                throw new NotImplementedException();
                        }
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Response is HttpWebResponse httpResponse)
                        throw new WebExceptionEx(ex, ct);
                    throw new AggregateException(Resources.Strings.ErrorDownloading, ex);
                }
                break;
            }

            ct.ThrowIfCancellationRequested();

            using (response)
            {
                // Read the data.
                var data = Array.Empty<byte>();
                try
                {
                    using (var stream = response.GetResponseStream())
                    {
                        var buffer = new byte[1048576];
                        try
                        {
                            for (; ; )
                            {
                                // Read data chunk.
                                var count = stream.Read(buffer, 0, buffer.Length, ct);
                                if (count == 0)
                                    break;

                                // Append it to the data.
                                var newData = new byte[data.LongLength + count];
                                Array.Copy(data, newData, data.LongLength);
                                data.Clear(0, data.LongLength);
                                Array.Copy(buffer, 0, newData, data.LongLength, count);
                                data = newData;
                            }
                        }
                        finally
                        {
                            buffer.Clear(0, buffer.Length);
                        }
                    }

                    if (res.PublicKeys != null)
                    {
                        // Generate signature URI.
                        var uriBuilderSig = new UriBuilder(res.Uri);
                        uriBuilderSig.Path += ".minisig";

                        // Create signature request.
                        request = CreateRequest(uriBuilderSig.Uri, token, "text/plain");

                        // Read the Minisign signature.
                        byte[] signature = null;
                        try
                        {
                            using (var responseSig = request.GetResponse())
                            {
                                // When request redirects are disabled, GetResponse() doesn't throw on 3xx status.
                                if (responseSig is HttpWebResponse httpResponseSig && httpResponseSig.StatusCode != HttpStatusCode.OK)
                                    throw new WebException("Response status code not 200", null, WebExceptionStatus.UnknownError, responseSig);

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
                        }
                        catch (WebException ex) { throw new AggregateException(Resources.Strings.ErrorDownloadingSignature, ex.Response is HttpWebResponse ? new WebExceptionEx(ex, ct) : ex); }

                        ct.ThrowIfCancellationRequested();

                        // Verify Minisign signature.
                        using (var s = new MemoryStream(signature, false))
                        using (var r = new BinaryReader(s))
                        {
                            if (r.ReadChar() != 'E')
                                throw new ArgumentException(Resources.Strings.ErrorUnsupportedMinisignSignature);
                            var alg = r.ReadChar();
                            var keyId = r.ReadUInt64();
                            if (!res.PublicKeys.ContainsKey(keyId))
                                throw new SecurityException(Resources.Strings.ErrorUntrustedMinisignPublicKey);
                            var sig = new byte[64];
                            if (r.Read(sig, 0, 64) != 64)
                                throw new ArgumentException(Resources.Strings.ErrorInvalidMinisignSignature);
                            var key = res.PublicKeys[keyId];
                            var payload =
                                alg == 'd' && (key.SupportedAlgorithms & MinisignPublicKey.AlgorithmMask.Legacy) != 0 ? data :
                                alg == 'D' && (key.SupportedAlgorithms & MinisignPublicKey.AlgorithmMask.Hashed) != 0 ? new eduLibsodium.BLAKE2b(512).ComputeHash(data) :
                                throw new ArgumentException(Resources.Strings.ErrorUnsupportedMinisignSignature);
                            using (var k = new eduLibsodium.ED25519(key.Value))
                                if (!k.VerifyDetached(payload, sig))
                                    throw new SecurityException(string.Format(Resources.Strings.ErrorInvalidSignature, res.Uri));
                        }
                    }

                    if (response is HttpWebResponse httpResponse)
                    {
                        var charset = httpResponse.CharacterSet;
                        var encoding = !string.IsNullOrEmpty(charset) ?
                            Encoding.GetEncoding(charset) :
                            Encoding.UTF8;
                        return new Response()
                        {
                            Value = encoding.GetString(data), // SECURITY: Securely convert data to a SecureString
                            ContentType = httpResponse.ContentType,
                            Date = DateTimeOffset.TryParse(httpResponse.GetResponseHeader("Date"), out var date) ? date : DateTimeOffset.Now,
                            Expires = DateTimeOffset.TryParse(httpResponse.GetResponseHeader("Expires"), out var expires) ? expires : DateTimeOffset.MaxValue,
                            LastModified = DateTimeOffset.TryParse(httpResponse.GetResponseHeader("Last-Modified"), out var lastModified) ? lastModified : DateTimeOffset.MinValue,
                            ETag = httpResponse.GetResponseHeader("ETag"),
                            Authorized = token != null ? token.Authorized : DateTimeOffset.MinValue,
                            IsFresh = true
                        };
                    }
                    else
                    {
                        return new Response()
                        {
                            Value = Encoding.UTF8.GetString(data), // SECURITY: Securely convert data to a SecureString
                            Authorized = token != null ? token.Authorized : DateTimeOffset.MinValue,
                            IsFresh = true
                        };
                    }
                }
                catch
                {
                    data.Clear(0, data.LongLength);
                    throw;
                }
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
            ContentType = reader[nameof(ContentType)];
            Date = DateTimeOffset.TryParse(reader[nameof(Date)], out var date) ? date : DateTimeOffset.Now;
            Expires = DateTimeOffset.TryParse(reader[nameof(Expires)], out var expires) ? expires : DateTimeOffset.MaxValue;
            LastModified = DateTimeOffset.TryParse(reader[nameof(LastModified)], out var lastModified) ? lastModified : DateTimeOffset.MinValue;
            ETag = reader[nameof(ETag)];
            Authorized = DateTimeOffset.TryParse(reader[nameof(Authorized)], out var authorized) ? authorized : DateTimeOffset.MinValue;
            IsFresh = (v = reader[nameof(IsFresh)]) != null && bool.TryParse(v, out var isFresh) && isFresh;
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> stream to which the object is serialized.</param>
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(nameof(Value), Value);
            if (ContentType != null)
                writer.WriteAttributeString(nameof(ContentType), ContentType);
            writer.WriteAttributeString(nameof(Date), Date.ToString("o"));
            if (Expires != DateTimeOffset.MaxValue)
                writer.WriteAttributeString(nameof(Expires), Expires.ToString("o"));
            if (LastModified != DateTimeOffset.MinValue)
                writer.WriteAttributeString(nameof(LastModified), LastModified.ToString("o"));
            if (ETag != null)
                writer.WriteAttributeString(nameof(ETag), ETag);
            if (Authorized != DateTimeOffset.MinValue)
                writer.WriteAttributeString(nameof(Authorized), Authorized.ToString("o"));
            writer.WriteAttributeString(nameof(IsFresh), IsFresh.ToString());
        }

        #endregion
    }
}

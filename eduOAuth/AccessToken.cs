/*
    eduOAuth - OAuth 2.0 Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduEx.Async;
using eduEx.System.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Web;

namespace eduOAuth
{
    /// <summary>
    /// Access token
    /// </summary>
    [Serializable]
    public class AccessToken : IDisposable, ISerializable
    {
        #region Fields

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly byte[] Entropy =
        {
            0x83, 0xb3, 0x15, 0xa2, 0x81, 0x57, 0x01, 0x0d, 0x8c, 0x21, 0x04, 0xd9, 0x11, 0xb3, 0xa7, 0x32,
            0xba, 0xb9, 0x8c, 0x15, 0x7b, 0x64, 0x32, 0x2b, 0x2f, 0x5f, 0x0e, 0x0d, 0xe5, 0x0a, 0x91, 0xc4,
            0x46, 0x81, 0xae, 0x72, 0xf6, 0xa7, 0x01, 0x67, 0x01, 0x91, 0x66, 0x1b, 0x5e, 0x5a, 0x51, 0xaa,
            0xbe, 0xf3, 0x23, 0x2a, 0x01, 0xc5, 0x8d, 0x01, 0x24, 0x56, 0x9b, 0xbd, 0xa6, 0xa3, 0x87, 0x87,
        };

        /// <summary>
        /// Access token
        /// </summary>
        protected SecureString Token;

        /// <summary>
        /// Refresh token
        /// </summary>
        private SecureString Refresh;

        #endregion

        #region Properties

        /// <summary>
        /// Timestamp of the initial authorization - advisory only; or <see cref="DateTimeOffset.MinValue"/> if unknown
        /// </summary>
        public DateTimeOffset Authorized { get; }

        /// <summary>
        /// Access token expiration date; or <see cref="DateTimeOffset.MaxValue"/> if token does not expire
        /// </summary>
        public DateTimeOffset Expires { get; }

        /// <summary>
        /// <see cref="true"/> if token is refreshable; or <see cref="false"/> otherwise
        /// </summary>
        public bool IsRefreshable => Refresh != null;

        /// <summary>
        /// List of access token scope identifiers
        /// </summary>
        public HashSet<string> Scope { get; private set; }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as AccessToken;
            if (!new NetworkCredential("", Token).Password.Equals(new NetworkCredential("", other.Token).Password))
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return
                new NetworkCredential("", Token).Password.GetHashCode();
        }

        /// <summary>
        /// Adds token to request
        /// </summary>
        /// <param name="request">Web request</param>
        public virtual void AddToRequest(WebRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Serializes access token to Base64 encoded string
        /// </summary>
        /// <returns>Serialized and Base64 encoded representation of access token</returns>
        public string ToBase64String()
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, this);
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        /// <summary>
        /// Deserialize access token from Base64 encoded string
        /// </summary>
        /// <param name="base64">Serialized and Base64 encoded representation of access token</param>
        /// <returns>Access token</returns>
        public static AccessToken FromBase64String(string base64)
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(base64)))
            {
                var formatter = new BinaryFormatter();
                return (AccessToken)formatter.Deserialize(stream);
            }
        }

        /// <summary>
        /// Parses authorization server response and creates an access token from it.
        /// </summary>
        /// <param name="request">Authorization server request</param>
        /// <param name="authorized">Timestamp of the initial authorization</param>
        /// <param name="scope">Expected scope</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Access token</returns>
        public static AccessToken FromAuthorizationServerResponse(WebRequest request, DateTimeOffset authorized, HashSet<string> scope = null, CancellationToken ct = default)
        {
            try
            {
                // Read and parse the response.
                using (var response = request.GetResponse())
                {
                    // When request redirects are disabled, GetResponse() doesn't throw on 3xx status.
                    if (response is HttpWebResponse httpResponse && httpResponse.StatusCode != HttpStatusCode.OK)
                        throw new WebException("Response status code not 200", null, WebExceptionStatus.UnknownError, response);
                    using (var responseStream = response.GetResponseStream())
                    using (var memstream = new MemoryStream())
                    {
                        responseStream.CopyTo(memstream);
                        var obj = Utf8Json.JsonSerializer.Deserialize<Json>(memstream.ToArray());

                        // Get token type and create the token based on the type.
                        AccessToken token = null;
                        switch (obj.token_type.ToLowerInvariant())
                        {
                            case "bearer": token = new BearerToken(obj, authorized); break;
                            default: throw new UnsupportedTokenTypeException(obj.token_type);
                        }

                        if (token.Scope == null && scope != null)
                        {
                            // The authorization server did not specify a token scope in response.
                            // The scope is assumed the same as have been requested.
                            token.Scope = scope;
                        }

                        return token;
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Response is HttpWebResponse httpResponse)
                {
                    if (httpResponse.StatusCode == HttpStatusCode.BadRequest && httpResponse.ContentType == "application/json")
                    {
                        // Parse server error.
                        using (var responseStream = httpResponse.GetResponseStream())
                        using (var memstream = new MemoryStream())
                        {
                            responseStream.CopyTo(memstream);
                            var obj = Utf8Json.JsonSerializer.Deserialize<JsonError>(memstream.ToArray());
                            throw new AccessTokenException(obj.error, obj.error_description, obj.error_uri);
                        }
                    }

                    throw new WebExceptionEx(ex, ct);
                }

                throw;
            }
        }

        /// <summary>
        /// Serializes access token to JSON encoded string for eduvpn-common
        /// </summary>
        /// <returns>JSON string</returns>
        public string ToJSON()
        {
            return string.Format("{{\"access_token\":\"{0}\",\"refresh_token\":\"{1}\",\"expires_at\":{2}}}",
                HttpUtility.JavaScriptStringEncode(new NetworkCredential("", Token).Password),
                HttpUtility.JavaScriptStringEncode(new NetworkCredential("", Refresh).Password),
                Expires.ToUnixTimeSeconds());
        }

        /// <summary>
        /// Uses the refresh token to obtain a new access token. The new access token is requested using the same scope as initially granted to the access token.
        /// </summary>
        /// <param name="request">Web request of the token endpoint used to obtain access token from authorization grant</param>
        /// <param name="clientId">Registered OAuth client ID (e.g. "org.eduvpn.app.windows")</param>
        /// <param name="clientCred">Client credentials (optional)</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Access token</returns>
        /// <remarks>
        /// <a href="https://tools.ietf.org/html/rfc6749#section-5.1">RFC6749 Section 5.1</a>,
        /// <a href="https://tools.ietf.org/html/rfc6749#section-6">RFC6749 Section 6</a>
        /// </remarks>
        public AccessToken RefreshToken(WebRequest request, string clientId, NetworkCredential clientCred = null, CancellationToken ct = default)
        {
            // Prepare token request body.
            var body =
                "grant_type=refresh_token" +
                "&refresh_token=" + Uri.EscapeDataString(new NetworkCredential("", Refresh).Password) +
                "&client_id=" + Uri.EscapeDataString(clientId);
            if (Scope != null)
                body += "&scope=" + Uri.EscapeDataString(string.Join(" ", Scope));

            // Send the request.
            request.Method = "POST";
            if (clientCred != null)
            {
                // Our client has credentials: requires authentication.
                request.Credentials = new CredentialCache
                {
                    { request.RequestUri, "Basic", clientCred }
                };
                request.PreAuthenticate = true;
            }
            var binBody = Encoding.ASCII.GetBytes(body);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = binBody.Length;
            using (var requestStream = request.GetRequestStream())
                requestStream.Write(binBody, 0, binBody.Length, ct);

            // Parse the response.
            var token = FromAuthorizationServerResponse(request, Authorized, Scope, ct);
            if (token.Refresh == null)
            {
                // The authorization server does not cycle the refresh tokens.
                // The refresh token remains the same.
                token.Refresh = Refresh;
            }

            return token;
        }

        /// <summary>
        /// Encrypts the data in a specified secure string and returns a byte array that contains the encrypted data
        /// </summary>
        /// <param name="userData">A secure string that contains data to encrypt</param>
        /// <returns>A byte array representing the encrypted data</returns>
        private static byte[] Protect(SecureString userData)
        {
            if (userData == null)
                throw new ArgumentNullException(nameof(userData));

            // Copy input to unmanaged string.
            var exposedInput = Marshal.SecureStringToGlobalAllocUnicode(userData);
            try
            {
                var data = new byte[userData.Length * sizeof(char)];
                try
                {
                    // Copy data.
                    for (int i = 0, n = data.Length; i < n; i++)
                        data[i] = Marshal.ReadByte(exposedInput, i);

                    // Encrypt!
                    return ProtectedData.Protect(
                        data,
                        Entropy,
                        DataProtectionScope.CurrentUser);
                }
                finally
                {
                    // Sanitize data.
                    for (long i = 0, n = data.LongLength; i < n; i++)
                        data[i] = 0;
                }
            }
            finally
            {
                // Sanitize memory.
                Marshal.ZeroFreeGlobalAllocUnicode(exposedInput);
            }
        }

        /// <summary>
        /// Decrypts the data in a specified byte array and returns a <see cref="SecureString"/> that contains the decrypted data
        /// </summary>
        /// <param name="encryptedData">A byte array containing data encrypted using the <c>System.Security.Cryptography.ProtectedData.Protect(System.Byte[],System.Byte[],System.Security.Cryptography.DataProtectionScope)</c> method.</param>
        /// <returns>A <see cref="SecureString"/> representing the decrypted data</returns>
        private static SecureString Unprotect(byte[] encryptedData)
        {
            // Decrypt data.
            var data = ProtectedData.Unprotect(encryptedData, Entropy, DataProtectionScope.CurrentUser);
            try
            {
                // Copy to SecureString.
                var output = new SecureString();
                for (long i = 0, n = data.LongLength; i < n; i += 2)
                    output.AppendChar((char)(data[i] + (((char)data[i + 1]) << 8)));
                output.MakeReadOnly();
                return output;
            }
            finally
            {
                // Sanitize data.
                for (long i = 0, n = data.LongLength; i < n; i++)
                    data[i] = 0;
            }
        }

        #endregion

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected AccessToken(SerializationInfo info, StreamingContext context)
        {
            // Load access token.
            Token = Unprotect((byte[])info.GetValue("Token", typeof(byte[])));

            // Load refresh token.
            byte[] refresh = null;
            try { refresh = (byte[])info.GetValue("Refresh", typeof(byte[])); }
            catch (SerializationException) { }
            Refresh = refresh != null ? Unprotect(refresh) : null;

            // Load other fields and properties.
            Authorized = DateTimeOffset.MinValue;
            try { Authorized = (DateTime)info.GetValue("Authorized", typeof(DateTime)); }
            catch (SerializationException) { }
            Expires = DateTimeOffset.MaxValue;
            try { Expires = (DateTime)info.GetValue("Expires", typeof(DateTime)); }
            catch (SerializationException) { }

            string[] scope = null;
            try { scope = (string[])info.GetValue("Scope", typeof(string[])); }
            catch (SerializationException) { }
            Scope = scope != null ? new HashSet<string>(scope) : null;
        }

        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to populate with data.</param>
        /// <param name="context">The destination for this serialization.</param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Save access token.
            info.AddValue("Token", Protect(Token));

            // Save refresh token.
            if (Refresh != null)
                info.AddValue("Refresh", Protect(Refresh));

            // Save other fields and properties.
            if (Authorized != DateTimeOffset.MinValue)
                info.AddValue("Authorized", Authorized.UtcDateTime);
            if (Expires != DateTimeOffset.MaxValue)
                info.AddValue("Expires", Expires.UtcDateTime);
            if (Scope != null)
                info.AddValue("Scope", Scope.ToArray());
        }

        #endregion

        #region IDisposable Support
        /// <summary>
        /// Flag to detect redundant <see cref="Dispose(bool)"/> calls.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool disposedValue = false;

        /// <summary>
        /// Called to dispose the object.
        /// </summary>
        /// <param name="disposing">Dispose managed objects</param>
        /// <remarks>
        /// To release resources for inherited classes, override this method.
        /// Call <c>base.Dispose(disposing)</c> within it to release parent class resources, and release child class resources if <paramref name="disposing"/> parameter is <c>true</c>.
        /// This method can get called multiple times for the same object instance. When the child specific resources should be released only once, introduce a flag to detect redundant calls.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Token != null)
                        Token.Dispose();

                    if (Refresh != null)
                        Refresh.Dispose();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="Dispose(bool)"/> with <c>disposing</c> parameter set to <c>true</c>.
        /// To implement resource releasing override the <see cref="Dispose(bool)"/> method.
        /// </remarks>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

        #region Utf8Json

        public class Json
        {
            public string token_type { get; set; }
            public string access_token { get; set; }
            public long? expires_at { get; set; }
            public long? expires_in { get; set; }
            public string refresh_token { get; set; }
            public string scope { get; set; }
        }

        public class JsonError
        {
            public string error {  get; set; }
            public string error_description { get; set; }
            public string error_uri { get; set; }
        }

        /// <summary>
        /// Initializes generic access token from data returned by authentication server.
        /// </summary>
        /// <param name="json">JSON object</param>
        /// <param name="authorized">Timestamp of the initial authorization</param>
        /// <remarks>
        /// <a href="https://tools.ietf.org/html/rfc6749#section-5.1">RFC6749 Section 5.1</a>
        /// </remarks>
        public AccessToken(Json json, DateTimeOffset authorized)
        {
            // Get access token.
            Token = new NetworkCredential("", json.access_token).SecurePassword;
            Token.MakeReadOnly();

            Authorized = authorized;

            // Get expiration date.
            Expires =
                json.expires_at.HasValue ? DateTimeOffset.FromUnixTimeSeconds(json.expires_at.Value) :
                json.expires_in.HasValue ? DateTimeOffset.Now.AddSeconds(json.expires_in.Value) : DateTimeOffset.MaxValue;

            // Get refresh token.
            if (json.refresh_token != null)
            {
                Refresh = new NetworkCredential("", json.refresh_token).SecurePassword;
                Refresh.MakeReadOnly();
            }

            // Get scope.
            if (json.scope != null)
                Scope = new HashSet<string>(json.scope.Split(null));
        }

        #endregion
    }
}

/*
    eduOAuth - OAuth 2.0 Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduEx.Async;
using eduEx.Security;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;

namespace eduOAuth
{
    /// <summary>
    /// OAuth authorization grant
    /// </summary>
    public class AuthorizationGrant : IDisposable
    {
        #region Data Types

        /// <summary>
        /// Code challenge algorithm method types
        /// </summary>
        /// <remarks>
        /// <a href="https://tools.ietf.org/html/rfc7636#section-4.2">RFC7636 Section 4.2</a>
        /// </remarks>
        public enum CodeChallengeAlgorithmType
        {
            /// <summary>
            /// PKCE disabled
            /// </summary>
            None,

            /// <summary>
            /// Code challenge = Code verifier
            /// </summary>
            Plain,

            /// <summary>
            /// Code challenge = Base64UrlEncodeNoPadding(SHA256(ASCII(Code verifier)))
            /// </summary>
            S256,
        }

        #endregion

        #region Fields

        /// <summary>
        /// PKCE code verifier
        /// </summary>
        private readonly SecureString CodeVerifier;

        #endregion

        #region Properties

        /// <summary>
        /// Authorization endpoint base URI
        /// </summary>
        public Uri AuthorizationEndpoint { get; private set; }

        /// <summary>
        /// Redirection endpoint base URI
        /// </summary>
        /// <remarks>Client should setup a listener on this URI prior this method is called.</remarks>
        public Uri RedirectEndpoint { get; set; }

        /// <summary>
        /// Client identifier
        /// </summary>
        /// <remarks>Should be populated before requesting authorization.</remarks>
        public string ClientId { get; private set; }

        /// <summary>
        /// Code challenge algorithm method
        /// </summary>
        /// <remarks>
        /// <a href="https://tools.ietf.org/html/rfc7636#section-4.2">RFC7636 Section 4.2</a>
        /// </remarks>
        public CodeChallengeAlgorithmType CodeChallengeAlgorithm { get; private set; }

        /// <summary>
        /// List of scope identifiers client is requesting access
        /// </summary>
        /// <remarks>Should be populated before requesting authorization. When empty, <c>scope</c> parameter is not included in authorization request URI.</remarks>
        public HashSet<string> Scope { get; private set; }

        /// <summary>
        /// Random client state
        /// </summary>
        public SecureString State { get; private set; }

        /// <summary>
        /// Authorization URI
        /// </summary>
        /// <remarks>
        /// <a href="https://tools.ietf.org/html/rfc6749#section-4.1.1">RFC6749 Section 4.1.1</a>,
        /// <a href="https://tools.ietf.org/html/rfc7636#section-4.1">RFC7636 Section 4.1</a>,
        /// <a href="https://tools.ietf.org/html/rfc7636#section-4.2">RFC7636 Section 4.2</a>,
        /// <a href="https://tools.ietf.org/html/rfc7636#section-4.3">RFC7636 Section 4.3</a>
        /// </remarks>
        public Uri AuthorizationUri
        {
            get
            {
                // Prepare authorization endpoint URI.
                var uriBuilder = new UriBuilder(AuthorizationEndpoint);
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);

                query["response_type"] = "code";
                query["client_id"] = ClientId;
                query["redirect_uri"] = RedirectEndpoint.ToString();

                if (Scope != null)
                {
                    // Add the client requested scope.
                    query["scope"] = string.Join(" ", Scope.ToArray());
                }

                // Add the random state.
                query["state"] = new NetworkCredential("", State).Password;

                if (CodeChallengeAlgorithm != CodeChallengeAlgorithmType.None)
                {
                    // Add the code challenge (RFC 7636).
                    switch (CodeChallengeAlgorithm)
                    {
                        case CodeChallengeAlgorithmType.Plain:
                            query["code_challenge_method"] = "plain";
                            query["code_challenge"] = new NetworkCredential("", CodeVerifier).Password;
                            break;

                        case CodeChallengeAlgorithmType.S256:
                            query["code_challenge_method"] = "S256";

                            {
                                var sha256 = new SHA256Managed();
                                query["code_challenge"] = Base64UrlEncodeNoPadding(sha256.ComputeHash(Encoding.ASCII.GetBytes(new NetworkCredential("", CodeVerifier).Password)));
                            }
                            break;
                    }
                }

                uriBuilder.Query = query.ToString();
                return uriBuilder.Uri;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an authorization grant.
        /// </summary>
        public AuthorizationGrant() :
            this(Array.Empty<byte>())
        {
        }

        /// <summary>
        /// Initializes an authorization grant.
        /// </summary>
        /// <param name="authorizationEndpoint">Authorization endpoint base URI</param>
        /// <param name="redirectEndpoint">Redirection endpoint base URI</param>
        /// <param name="clientId">Registered OAuth client ID (e.g. "org.eduvpn.app.windows")</param>
        /// <param name="scope">Should be populated before requesting authorization. When empty, <c>scope</c> parameter is not included in authorization request URI.</param>
        /// <param name="codeChallengeAlgorithm">Code challenge algorithm method</param>
        public AuthorizationGrant(Uri authorizationEndpoint, Uri redirectEndpoint, string clientId, HashSet<string> scope, CodeChallengeAlgorithmType codeChallengeAlgorithm = CodeChallengeAlgorithmType.S256) :
            this(Array.Empty<byte>(), codeChallengeAlgorithm)
        {
            AuthorizationEndpoint = authorizationEndpoint;
            RedirectEndpoint = redirectEndpoint;
            ClientId = clientId;
            Scope = scope;
        }

        /// <summary>
        /// Initializes an authorization grant.
        /// </summary>
        /// <param name="statePrefix">Data to prefix OAuth state with to allow disambiguation between multiple concurrent authorization requests</param>
        /// <param name="codeChallengeAlgorithm">Code challenge algorithm method</param>
        public AuthorizationGrant(byte[] statePrefix, CodeChallengeAlgorithmType codeChallengeAlgorithm = CodeChallengeAlgorithmType.S256)
        {
            CodeChallengeAlgorithm = codeChallengeAlgorithm;

            var rng = new RNGCryptoServiceProvider();
            var random = new byte[32];
            try
            {
                // Calculate random state.
                rng.GetBytes(random);
                var state = new byte[statePrefix.LongLength + random.LongLength];
                try
                {
                    Array.Copy(statePrefix, 0, state, 0, statePrefix.LongLength);
                    Array.Copy(random, 0, state, statePrefix.LongLength, random.LongLength);
                    State = new NetworkCredential("", Base64UrlEncodeNoPadding(state)).SecurePassword;
                    State.MakeReadOnly();
                }
                finally
                {
                    // Sanitize!
                    for (long i = 0, n = state.LongLength; i < n; i++)
                        state[i] = 0;
                }

                // Calculate code verifier.
                rng.GetBytes(random);
                CodeVerifier = new NetworkCredential("", Base64UrlEncodeNoPadding(random)).SecurePassword;
                CodeVerifier.MakeReadOnly();
            }
            finally
            {
                // Sanitize!
                for (long i = 0, n = random.LongLength; i < n; i++)
                    random[i] = 0;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Parses authorization grant received and requests access token if successful.
        /// </summary>
        /// <param name="redirectResponse">Parameters of the access grant</param>
        /// <param name="request">Web request of the token endpoint used to obtain access token from authorization grant</param>
        /// <param name="clientSecret">Client secret (optional)</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Access token</returns>
        /// <remarks>
        /// <a href="https://tools.ietf.org/html/rfc6749#section-5.2">RFC6749 Section 5.2</a>,
        /// <a href="https://tools.ietf.org/html/rfc6749#section-4.1.2">RFC6749 Section 4.1.2</a>,
        /// <a href="https://tools.ietf.org/html/rfc6749#section-4.1.2.1">RFC6749 Section 4.1.2.1</a>,
        /// <a href="https://tools.ietf.org/html/rfc6749#section-4.1.3">RFC6749 Section 4.1.3</a>,
        /// <a href="https://tools.ietf.org/html/rfc6749#section-4.1.4">RFC6749 Section 4.1.4</a>,
        /// <a href="https://tools.ietf.org/html/rfc6749#section-5.2">RFC6749 Section 5.2</a>,
        /// <a href="https://tools.ietf.org/html/rfc7636#section-4.5">RFC7636 Section 4.5</a>
        /// </remarks>
        public AccessToken ProcessResponse(NameValueCollection redirectResponse, WebRequest request, SecureString clientSecret = null, CancellationToken ct = default)
        {
            // Verify state parameter to be present and matching.
            var responseState = redirectResponse["state"];
            if (responseState == null)
                throw new eduJSON.MissingParameterException("state");
            if (!new NetworkCredential("", responseState).SecurePassword.IsEqualTo(State))
                throw new InvalidStateException();

            // Did authorization server report an error?
            var responseError = redirectResponse["error"];
            if (responseError != null)
                throw new AuthorizationGrantException(responseError, redirectResponse["error_description"], redirectResponse["error_uri"]);

            // Verify authorization code to be present.
            var authorizationCode = redirectResponse["code"]/*.Replace(' ', '+') <= IE11 sends URI unescaped causing + to get converted into space. The issue is avoided by switching to Base64UrlEncodeNoPadding encoding.*/;
            if (authorizationCode == null)
                throw new eduJSON.MissingParameterException("code");

            // Prepare token request body.
            var body =
                "grant_type=authorization_code" +
                "&code=" + Uri.EscapeDataString(authorizationCode) +
                "&redirect_uri=" + Uri.EscapeDataString(RedirectEndpoint.ToString()) +
                "&client_id=" + Uri.EscapeDataString(ClientId);
            if (CodeVerifier != null)
                body += "&code_verifier=" + new NetworkCredential("", CodeVerifier).Password;

            // Send the request.
            request.Method = "POST";
            if (clientSecret != null)
            {
                // Our client has credentials: requires authentication.
                request.Credentials = new CredentialCache
                {
                    { request.RequestUri, "Basic", new NetworkCredential(ClientId, clientSecret) }
                };
                request.PreAuthenticate = true;
            }
            var binBody = Encoding.ASCII.GetBytes(body);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = binBody.Length;
            using (var requestStream = request.GetRequestStream())
                requestStream.Write(binBody, 0, binBody.Length, ct);

            // Parse the response.
            return AccessToken.FromAuthorizationServerResponse(request, DateTimeOffset.Now, Scope, ct);
        }

        /// <summary>
        /// Encodes binary data for RFC 7636 request.
        /// </summary>
        /// <param name="data">Data to encode</param>
        /// <returns>Encoded string</returns>
        /// <remarks>
        /// <a href="https://tools.ietf.org/html/rfc7636#appendix-A">RFC7636 Appendix A</a>
        /// </remarks>
        public static string Base64UrlEncodeNoPadding(byte[] data)
        {
            var s = Convert.ToBase64String(data); // Regular Base64 encoder
            s = s.Split('=')[0]; // Remove any trailing '='s
            s = s.Replace('+', '-'); // 62nd char of encoding
            s = s.Replace('/', '_'); // 63rd char of encoding
            return s;
        }

        /// <summary>
        /// Decodes string for RFC 7636 request.
        /// </summary>
        /// <param name="data">String to decode</param>
        /// <returns>Decoded data</returns>
        public static byte[] Base64UriDecodeNoPadding(string data)
        {
            var s = data.Replace('_', '/'); // 63rd char of encoding
            s = s.Replace('-', '+'); // 62nd char of encoding
            s = s.PadRight(s.Length + ((4 - s.Length) & 0x3), '='); // Add trailing '='s
            return Convert.FromBase64String(s); // Regular Base64 decoder
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
                    if (State != null)
                        State.Dispose();

                    if (CodeVerifier != null)
                        CodeVerifier.Dispose();
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
    }
}

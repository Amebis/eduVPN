/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Web;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN server
    /// </summary>
    public class Server : JSON.ILoadableItem
    {
        #region Fields

        /// <summary>
        /// Server API endpoints
        /// </summary>
        private ServerEndpoints Endpoints;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object EndpointsLock = new object();

        /// <summary>
        /// List of available profiles
        /// </summary>
        private ObservableCollection<Profile> Profiles;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object ProfilesLock = new object();

        #endregion

        #region Properties

        /// <summary>
        /// PEM encoded client certificate path
        /// </summary>
        public string ClientCertificatePath
        {
            get => Path.Combine(
                Path.GetDirectoryName(Path.GetDirectoryName(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath)),
                "certs",
                Base.Host + ".pem");
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object ClientCertificateLock = new object();

        /// <summary>
        /// Server base URI
        /// </summary>
        public Uri Base { get; private set; }

        /// <summary>
        /// List of support contact URLs
        /// </summary>
        public List<Uri> SupportContacts { get; } = new List<Uri>();

        /// <summary>
        /// Request authorization event
        /// </summary>
        /// <remarks>Sender is the authenticating server <see cref="eduVPN.Models.Server"/>.</remarks>
        public event EventHandler<RequestAuthorizationEventArgs> RequestAuthorization;

        /// <summary>
        /// Called when a profile requests user authorization
        /// </summary>
        /// <param name="authenticatingServer">Authenticating server</param>
        /// <param name="e"><see cref="RequestAuthorization"/> event arguments</param>
        public void OnRequestAuthorization(Server authenticatingServer, RequestAuthorizationEventArgs e)
        {
            RequestAuthorization?.Invoke(authenticatingServer, e);

            if (e.AccessToken is eduOAuth.InvalidToken)
                throw new InvalidAccessTokenException(string.Format(Resources.Strings.ErrorInvalidAccessToken, this));
        }

        /// <summary>
        /// Forget authorization event
        /// </summary>
        /// <remarks>Sender is the authenticating server <see cref="eduVPN.Models.Server"/>.</remarks>
        public event EventHandler<ForgetAuthorizationEventArgs> ForgetAuthorization;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a server
        /// </summary>
        public Server()
        {
        }

        /// <summary>
        /// Constructs a custom server
        /// </summary>
        /// <param name="b">Server base URI</param>
        public Server(Uri b) : this()
        {
            // Set base.
            Base = b;
        }

        /// <summary>
        /// Constructs the authenticating server
        /// </summary>
        /// <param name="authorizationEndpoint">Authorization endpoint URI - used by the client to obtain authorization from the resource owner via user-agent redirection.</param>
        /// <param name="tokenEndpoint">Token endpoint URI - used by the client to exchange an authorization grant for an access token, typically with client authentication.</param>
        public Server(Uri authorizationEndpoint, Uri tokenEndpoint) : this()
        {
            // Make a guess for the base name - required to identify server (e.g. when caching access tokens).
            Base = new Uri(authorizationEndpoint.GetLeftPart(UriPartial.Authority) + "/");

            // Set API endpoints manually.
            Endpoints = new ServerEndpoints(authorizationEndpoint, tokenEndpoint);
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return Base.Host;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as Server;
            if (!Base.Equals(other.Base))
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return
                Base.GetHashCode();
        }

        /// <summary>
        /// Gets and loads server endpoints
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Server endpoints</returns>
        public ServerEndpoints GetEndpoints(CancellationToken ct = default)
        {
            lock (EndpointsLock)
            {
                if (Endpoints != null)
                    return Endpoints;

                try
                {
                    // Get and load API endpoints.
                    var endpoints = new ServerEndpoints();
                    var uriBuilder = new UriBuilder(Base);
                    uriBuilder.Path += "info.json";
                    endpoints.LoadJSON(Xml.Response.Get(
                        uri: uriBuilder.Uri,
                        ct: ct).Value, ct);
                    return Endpoints = endpoints;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorEndpointsLoad, ex); }
            }
        }

        /// <summary>
        /// Gets server profile list available to the user
        /// </summary>
        /// <param name="authenticatingServer">Authenticating server (can be same as this server)</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Profile list</returns>
        public ObservableCollection<Profile> GetProfileList(Server authenticatingServer, CancellationToken ct = default)
        {
            lock (ProfilesLock)
            {
                if (Profiles != null)
                    return Profiles;

                // Get API endpoints.
                var api = GetEndpoints(ct);
                var e = new RequestAuthorizationEventArgs("config");

                retry:
                // Request authentication token.
                OnRequestAuthorization(authenticatingServer, e);

                try
                {
                    // Parse JSON string and get inner key/value dictionary.
                    var obj = eduJSON.Parser.GetValue<Dictionary<string, object>>(
                        (Dictionary<string, object>)eduJSON.Parser.Parse(Xml.Response.Get(
                            uri: api.Profiles,
                            token: e.AccessToken,
                            ct: ct).Value, ct),
                        "profile_list");

                    // Verify response status.
                    if (eduJSON.Parser.GetValue(obj, "ok", out bool ok) && !ok)
                        throw new APIErrorException();

                    // Load collection.
                    if (!(obj["data"] is List<object> obj2))
                        throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(List<object>), obj.GetType());

                    // Parse all items listed. Don't do it in parallel to preserve the sort order.
                    var profileList = new ObservableCollection<Profile>();
                    foreach (var el in obj2)
                    {
                        var profile = new Profile(this);
                        profile.Load(el);

                        // Attach to RequestAuthorization profile events.
                        profile.RequestAuthorization += (object sender_profile, RequestAuthorizationEventArgs e_profile) => OnRequestAuthorization(authenticatingServer, e_profile);

                        profileList.Add(profile);
                    }
                    return Profiles = profileList;
                }
                catch (OperationCanceledException) { throw; }
                catch (WebException ex)
                {
                    if (ex.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        // Access token was rejected (401 Unauthorized).
                        if (e.TokenOrigin == RequestAuthorizationEventArgs.TokenOriginType.Saved)
                        {
                            // Access token loaded from the settings was rejected.
                            // This might happen when ill-clocked client thinks the token is still valid, but the server expired it already.
                            // Retry with forced access token refresh.
                            e.ForceRefresh = true;
                            goto retry;
                        }
                    }
                    throw new AggregateException(Resources.Strings.ErrorProfileListLoad, ex);
                }
                catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorProfileListLoad, ex); }
            }
        }

        /// <summary>
        /// Gets client certificate
        /// </summary>
        /// <param name="authenticatingServer">Authenticating server (can be same as this server)</param>
        /// <param name="forceRefresh">Force new certificate creation</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Client certificate</returns>
        public X509Certificate2 GetClientCertificate(Server authenticatingServer, bool forceRefresh = false, CancellationToken ct = default)
        {
            lock (ClientCertificateLock)
            {
                var path = ClientCertificatePath;

                // Get API endpoints.
                var api = GetEndpoints(ct);
                var e = new RequestAuthorizationEventArgs("config");
                if (forceRefresh)
                    e.SourcePolicy = RequestAuthorizationEventArgs.SourcePolicyType.ForceAuthorization;

                retry:
                // Request authentication token.
                OnRequestAuthorization(authenticatingServer, e);

                if (!forceRefresh && File.Exists(path))
                {
                    // Perform an optional certificate check.
                    try
                    {
                        // Load certificate.
                        var cert = new X509Certificate2(
                            Certificate.GetBytesFromPEM(
                                File.ReadAllText(path),
                                "CERTIFICATE"),
                            (string)null,
                            X509KeyStorageFlags.PersistKeySet);

                        // Check certificate.
                        var uriBuilder = new UriBuilder(api.CheckCertificate);
                        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                        query["common_name"] = cert.GetNameInfo(X509NameType.SimpleName, false);
                        uriBuilder.Query = query.ToString();
                        var certCheck = new CertificateCheck();
                        certCheck.LoadJSONAPIResponse(Xml.Response.Get(
                            uri: uriBuilder.Uri,
                            token: e.AccessToken,
                            ct: ct).Value, "check_certificate", ct);

                        switch (certCheck.Result)
                        {
                            case CertificateCheck.ReasonType.Valid:
                                // Certificate is valid.
                                return cert;

                            case CertificateCheck.ReasonType.UserDisabled:
                                throw new CertificateCheckException(Resources.Strings.ErrorUserDisabled);

                            case CertificateCheck.ReasonType.CertificateDisabled:
                                throw new CertificateCheckException(Resources.Strings.ErrorCertificateDisabled);

                            default:
                                // Server reported it will not accept this certificate.
                                break;
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (CertificateCheckException) { throw; }
                    catch (WebException ex)
                    {
                        if (ex.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            // Access token was rejected (401 Unauthorized).
                            if (e.TokenOrigin == RequestAuthorizationEventArgs.TokenOriginType.Saved)
                            {
                                // Access token loaded from the settings was rejected.
                                // This might happen when ill-clocked client thinks the token is still valid, but the server expired it already.
                                // Retry with forced access token refresh.
                                e.ForceRefresh = true;
                                goto retry;
                            }
                        }
                    }
                    catch (Exception) { }
                }

                try
                {
                    // Get certificate and save it.
                    var cert = new Certificate();
                    cert.LoadJSONAPIResponse(Xml.Response.Get(
                        uri: api.CreateCertificate,
                        param: new NameValueCollection
                        {
                            { "display_name", string.Format("{0} Client for Windows", Properties.Settings.Default.ClientTitle) } // Always use English display_name
                        },
                        token: e.AccessToken,
                        ct: ct).Value, "create_keypair", ct);

                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using (StreamWriter file = new StreamWriter(path))
                    {
                        file.Write(cert.Cert);
                        file.Write("\n");
                        file.Write(new NetworkCredential("", cert.PrivateKey).Password);
                        file.Write("\n");
                    }

                    var certX509 = new X509Certificate2(
                        Certificate.GetBytesFromPEM(
                            cert.Cert,
                            "CERTIFICATE"),
                        (string)null,
                        X509KeyStorageFlags.PersistKeySet);
                    return certX509;
                }
                catch (OperationCanceledException) { throw; }
                catch (WebException ex)
                {
                    if (ex.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        // Access token was rejected (401 Unauthorized).
                        if (e.TokenOrigin == RequestAuthorizationEventArgs.TokenOriginType.Saved)
                        {
                            // Access token loaded from the settings was rejected.
                            // This might happen when ill-clocked client thinks the token is still valid, but the server expired it already.
                            // Retry with forced access token refresh.
                            e.ForceRefresh = true;
                            goto retry;
                        }
                    }
                    throw new AggregateException(Resources.Strings.ErrorClientCertificateLoad, ex);
                }
                catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorClientCertificateLoad, ex); }
            }
        }

        /// <summary>
        /// Removes server data from cache
        /// </summary>
        public void Forget()
        {
            lock (ProfilesLock)
            {
                // Remove profile list from cache.
                Profiles = null;
            }

            lock (ClientCertificateLock)
            {
                // Remove previously issued client certificate.
                try { File.Delete(ClientCertificatePath); } catch { }
            }

            // Ask authorization provider to forget our authorization token.
            ForgetAuthorization?.Invoke(this, new ForgetAuthorizationEventArgs("config"));
        }

        #endregion

        #region ILoadableItem Support

        /// <summary>
        /// Loads server from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>server_type</c>, <c>base_url</c>, <c>display_name</c>, <c>country_code</c> and <c>support_contact</c> elements.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public virtual void Load(object obj)
        {
            if (!(obj is Dictionary<string, object> obj2))
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());

            // Set base URI.
            Endpoints = null;
            Profiles = null;
            Base = new Uri(eduJSON.Parser.GetValue<string>(obj2, "base_url"));

            // Set support contact URLs.
            SupportContacts.Clear();
            if (eduJSON.Parser.GetValue(obj2, "support_contact", out List<object> support_contact))
                foreach (var c in support_contact)
                    if (c is string cStr)
                        SupportContacts.Add(new Uri(cStr));
        }

        #endregion
    }
}

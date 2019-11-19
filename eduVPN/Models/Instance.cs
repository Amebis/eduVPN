/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Web;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN instance (VPN service provider)
    /// </summary>
    public class Instance : BindableBase, JSON.ILoadableItem
    {
        #region Fields

        /// <summary>
        /// Instance API endpoints
        /// </summary>
        private InstanceEndpoints _endpoints;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object _endpoints_lock = new object();

        /// <summary>
        /// List of available profiles
        /// </summary>
        private ObservableCollection<Profile> _profile_list;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object _profile_list_lock = new object();

        /// <summary>
        /// Client certificate
        /// </summary>
        private X509Certificate2 _client_certificate;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object _client_certificate_lock = new object();

        #endregion

        #region Properties

        /// <summary>
        /// Instance base URI
        /// </summary>
        public Uri Base
        {
            get { return _base; }
            set {
                if (value != _base)
                {
                    // Setting the base resets internal state (fields).
                    _endpoints = null;
                    _profile_list = null;
                    _client_certificate = null;

                    SetProperty(ref _base, value);
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Uri _base;

        /// <summary>
        /// Instance name to display in GUI
        /// </summary>
        public string DisplayName
        {
            get { return _display_name; }
            set { SetProperty(ref _display_name, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _display_name;

        /// <summary>
        /// Instance logo URI
        /// </summary>
        public Uri Logo
        {
            get { return _logo; }
            set { SetProperty(ref _logo, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Uri _logo;

        /// <summary>
        /// Popularity factor in the [0.0, 1.0] range (default 1.0)
        /// </summary>
        public float Popularity
        {
            get { return _popularity; }
            set { SetProperty(ref _popularity, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _popularity = 1.0f;

        /// <summary>
        /// Request authorization event
        /// </summary>
        /// <remarks>Sender is the authenticating instance <see cref="eduVPN.Models.Instance"/>.</remarks>
        public event EventHandler<RequestAuthorizationEventArgs> RequestAuthorization;

        /// <summary>
        /// Called when a profile requests user authorization
        /// </summary>
        /// <param name="authenticating_instance">Authenticating instance</param>
        /// <param name="e"><see cref="RequestAuthorization"/> event arguments</param>
        public void OnRequestAuthorization(Instance authenticating_instance, RequestAuthorizationEventArgs e)
        {
            RequestAuthorization?.Invoke(authenticating_instance, e);
        }

        /// <summary>
        /// Forget authorization event
        /// </summary>
        /// <remarks>Sender is the authenticating instance <see cref="eduVPN.Models.Instance"/>.</remarks>
        public event EventHandler<ForgetAuthorizationEventArgs> ForgetAuthorization;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance
        /// </summary>
        public Instance() :
            base()
        {
        }

        /// <summary>
        /// Constructs a custom instance
        /// </summary>
        /// <param name="b">Instance base URI</param>
        public Instance(Uri b)
        {
            // Set base.
            _base = b;

            // Set display name to base URI hostname.
            _display_name = b.Host;

            // Set instance logo to /favicon.ico, perhaps we might get lucky.
            _logo = new UriBuilder(b) { Path = "/favicon.ico" }.Uri;
        }

        /// <summary>
        /// Constructs the authenticating instance
        /// </summary>
        /// <param name="authorization_endpoint">Authorization endpoint URI - used by the client to obtain authorization from the resource owner via user-agent redirection.</param>
        /// <param name="token_endpoint">Token endpoint URI - used by the client to exchange an authorization grant for an access token, typically with client authentication.</param>
        public Instance(Uri authorization_endpoint, Uri token_endpoint) :
            this()
        {
            // Make a guess for the base name - required to identify instance (e.g. when caching access tokens).
            _base = new Uri(authorization_endpoint.GetLeftPart(UriPartial.Authority) + "/");

            // Set display name to authorization URI hostname.
            _display_name = authorization_endpoint.Host;

            // Set instance logo to /favicon.ico, perhaps we might get lucky.
            _logo = new UriBuilder(authorization_endpoint) { Path = "/favicon.ico" }.Uri;

            // Set API endpoints manually.
            _endpoints = new InstanceEndpoints()
            {
                AuthorizationEndpoint = authorization_endpoint,
                TokenEndpoint = token_endpoint,
            };
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return DisplayName;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as Instance;
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
        /// Gets and loads instance endpoints
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Instance endpoints</returns>
        public InstanceEndpoints GetEndpoints(CancellationToken ct = default(CancellationToken))
        {
            lock (_endpoints_lock)
            {
                if (_endpoints == null)
                {
                    try
                    {
                        // Get and load API endpoints.
                        var endpoints = new InstanceEndpoints();
                        var uri_builder = new UriBuilder(Base);
                        uri_builder.Path += "info.json";
                        endpoints.LoadJSON(Xml.Response.Get(
                            uri: uri_builder.Uri,
                            ct: ct).Value, ct);

                        // If we got here, save the endpoints.
                        _endpoints = endpoints;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorEndpointsLoad, ex); }
                }

                return _endpoints;
            }
        }

        /// <summary>
        /// Gets instance profile list available to the user
        /// </summary>
        /// <param name="authenticating_instance">Authenticating instance (can be same as this instance)</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Profile list</returns>
        public ObservableCollection<Profile> GetProfileList(Instance authenticating_instance, CancellationToken ct = default(CancellationToken))
        {
            lock (_profile_list_lock)
            {
                if (_profile_list == null)
                {
                    // Get API endpoints.
                    var api = GetEndpoints(ct);
                    var e = new RequestAuthorizationEventArgs("config");

                    retry:
                    // Request authentication token.
                    OnRequestAuthorization(authenticating_instance, e);
                    if (e.AccessToken == null)
                        throw new AccessTokenNullException();

                    try
                    {
                        // Get and load profile list.
                        var profile_list = new ObservableCollection<Profile>();
                        profile_list.LoadJSONAPIResponse(Xml.Response.Get(
                            uri: api.ProfileList,
                            token: e.AccessToken,
                            ct: ct).Value, "profile_list", ct);

                        foreach (var profile in profile_list)
                        {
                            // Bind profile to our instance.
                            profile.Instance = this;

                            // Attach to RequestAuthorization profile events.
                            profile.RequestAuthorization += (object sender_profile, RequestAuthorizationEventArgs e_profile) => OnRequestAuthorization(authenticating_instance, e_profile);
                        }

                        // If we got here, save the profile list.
                        _profile_list = profile_list;
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
                            }
                            else
                            {
                                // Retry with forced authorization, ignoring saved access token completely.
                                e.SourcePolicy = RequestAuthorizationEventArgs.SourcePolicyType.ForceAuthorization;
                            }
                            goto retry;
                        }
                        else
                            throw new AggregateException(Resources.Strings.ErrorProfileListLoad, ex);
                    }
                    catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorProfileListLoad, ex); }
                }

                return _profile_list;
            }
        }

        /// <summary>
        /// Gets client certificate
        /// </summary>
        /// <param name="authenticating_instance">Authenticating instance (can be same as this instance)</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Client certificate. Certificate (including the private key) is saved to user certificate store.</returns>
        public X509Certificate2 GetClientCertificate(Instance authenticating_instance, CancellationToken ct = default(CancellationToken))
        {
            lock (_client_certificate_lock)
            {
                if (_client_certificate == null)
                {
                    // Open eduVPN client certificate store.
                    var store = new X509Store(Properties.Settings.Default.ClientID, StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadWrite);
                    try
                    {
                        // Try to restore previously issued client certificate from the certificate store first.
                        var friendly_name = Base.AbsoluteUri;
                        foreach (var cert in store.Certificates)
                        {
                            var cert_friendly_name = cert.FriendlyName;
                            if (DateTime.Now < cert.NotAfter && cert.HasPrivateKey && cert_friendly_name.Length > 0)
                            {
                                // Not expired && Has the private key.
                                if (cert_friendly_name == friendly_name)
                                {
                                    // Certificate found.
                                    _client_certificate = cert;
                                }
                            }
                            else
                            {
                                // Certificate expired or matching private key not found or without a name == Useless. Clean it from the store.
                                store.Remove(cert);
                            }
                        }
                    }
                    finally { store.Close(); }
                }

                if (_client_certificate != null)
                {
                    // Get API endpoints.
                    var api = GetEndpoints(ct);
                    var e = new RequestAuthorizationEventArgs("config");

                    retry:
                    // Request authentication token.
                    OnRequestAuthorization(authenticating_instance, e);
                    if (e.AccessToken == null)
                        throw new AccessTokenNullException();

                    // Perform an optional certificate check.
                    try
                    {
                        // Check certificate.
                        var uri_builder = new UriBuilder(api.CheckCertificate);
                        var query = HttpUtility.ParseQueryString(uri_builder.Query);
                        query["common_name"] = _client_certificate.GetNameInfo(X509NameType.SimpleName, false);
                        uri_builder.Query = query.ToString();
                        var cert_check = new CertificateCheck();
                        cert_check.LoadJSONAPIResponse(Xml.Response.Get(
                            uri: uri_builder.Uri,
                            token: e.AccessToken,
                            ct: ct).Value, "check_certificate", ct);

                        switch (cert_check.Result)
                        {
                            case CertificateCheck.ReasonType.Valid:
                                // Certificate is valid.
                                break;

                            case CertificateCheck.ReasonType.UserDisabled:
                                throw new CertificateCheckException(Resources.Strings.ErrorUserDisabled);

                            case CertificateCheck.ReasonType.CertificateDisabled:
                                throw new CertificateCheckException(Resources.Strings.ErrorCertificateDisabled);

                            default:
                                // Server reported it will not accept this certificate.
                                _client_certificate = null;
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
                            }
                            else
                            {
                                // Retry with forced authorization, ignoring saved access token completely.
                                e.SourcePolicy = RequestAuthorizationEventArgs.SourcePolicyType.ForceAuthorization;
                            }
                            goto retry;
                        }
                    }
                    catch (Exception) { }
                }

                if (_client_certificate == null)
                {
                    // Get API endpoints.
                    var api = GetEndpoints(ct);
                    var e = new RequestAuthorizationEventArgs("config");

                    retry:
                    // Request authentication token.
                    OnRequestAuthorization(authenticating_instance, e);
                    if (e.AccessToken == null)
                        throw new AccessTokenNullException();

                    // Open eduVPN client certificate store.
                    var store = new X509Store(Properties.Settings.Default.ClientID, StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadWrite);
                    try
                    {
                        // Get certificate and import it to Windows user certificate store.
                        var cert = new Certificate();
                        cert.LoadJSONAPIResponse(Xml.Response.Get(
                            uri: api.CreateCertificate,
                            param: new NameValueCollection
                            {
                                { "display_name", String.Format("{0} Client for Windows", Properties.Settings.Default.ClientTitle) } // Always use English display_name
                            },
                            token: e.AccessToken,
                            ct: ct).Value, "create_keypair", ct);
                        cert.Value.FriendlyName = Base.AbsoluteUri;
                        store.Add(cert.Value);

                        // If we got here, save the certificate.
                        _client_certificate = cert.Value;
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
                            }
                            else
                            {
                                // Retry with forced authorization, ignoring saved access token completely.
                                e.SourcePolicy = RequestAuthorizationEventArgs.SourcePolicyType.ForceAuthorization;
                            }
                            goto retry;
                        }
                        else
                            throw new AggregateException(Resources.Strings.ErrorClientCertificateLoad, ex);
                    }
                    catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorClientCertificateLoad, ex); }
                    finally { store.Close(); }
                }

                return _client_certificate;
            }
        }

        /// <summary>
        /// Refreshes client certificate
        /// </summary>
        /// <param name="authenticating_instance">Authenticating instance (can be same as this instance)</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Client certificate. Certificate (including the private key) is saved to user certificate store.</returns>
        public X509Certificate2 RefreshClientCertificate(Instance authenticating_instance, CancellationToken ct = default(CancellationToken))
        {
            lock (_client_certificate_lock)
            {
                var friendly_name = Base.AbsoluteUri;

                // Open eduVPN client certificate store.
                var store = new X509Store(Properties.Settings.Default.ClientID, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                try
                {
                    // Delete previously issued client certificate from the certificate store.
                    foreach (var cert in store.Certificates)
                        if (cert.FriendlyName == friendly_name)
                            store.Remove(cert);
                }
                finally { store.Close(); }

                // Invalidate memory cache.
                _client_certificate = null;

                return GetClientCertificate(authenticating_instance, ct);
            }
        }

        /// <summary>
        /// Removes instance data from cache
        /// </summary>
        public void Forget()
        {
            lock (_profile_list_lock)
            {
                // Remove profile list from cache.
                _profile_list = null;
            }

            lock (_client_certificate_lock)
            {
                // Open eduVPN client certificate store.
                var store = new X509Store(Properties.Settings.Default.ClientID, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                try
                {
                    // Remove previously issued client certificate from the certificate store.
                    var friendly_name = Base.AbsoluteUri;
                    foreach (var cert in store.Certificates)
                        if (cert.FriendlyName == friendly_name)
                            store.Remove(cert);
                }
                finally { store.Close(); }

                _client_certificate = null;
            }

            // Ask authorization provider to forget our authorization token.
            ForgetAuthorization?.Invoke(this, new ForgetAuthorizationEventArgs("config"));
        }

        #endregion

        #region ILoadableItem Support

        /// <summary>
        /// Loads instance from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>base_uri</c>, <c>logo</c> and <c>display_name</c> elements. <c>base_uri</c> is required. All elements should be strings.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public void Load(object obj)
        {
            if (obj is Dictionary<string, object> obj2)
            {
                // Set base URI.
                Base = new Uri(eduJSON.Parser.GetValue<string>(obj2, "base_uri"));

                // Set display name.
                DisplayName = eduJSON.Parser.GetLocalizedValue(obj2, "display_name", out string display_name) ? display_name : Base.Host;

                // Set logo URI.
                Logo = eduJSON.Parser.GetLocalizedValue(obj2, "logo", out string logo_uri) ? new Uri(logo_uri) : null;
            }
            else
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());
        }

        #endregion
    }
}

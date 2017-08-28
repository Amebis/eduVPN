/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using eduVPN.JSON;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN instance (VPN service provider) basic info
    /// </summary>
    public class InstanceInfo : BindableBase, JSON.ILoadableItem, IXmlSerializable
    {
        #region Fields

        /// <summary>
        /// Instance API endpoints
        /// </summary>
        private InstanceEndpoints _endpoints;
        private object _endpoints_lock = new object();

        /// <summary>
        /// Access token
        /// </summary>
        private AccessToken _access_token;
        private object _access_token_lock = new object();

        /// <summary>
        /// List of available profiles
        /// </summary>
        private JSON.Collection<Models.ProfileInfo> _profile_list;
        private object _profile_list_lock = new object();

        /// <summary>
        /// Client certificate
        /// </summary>
        private X509Certificate2 _client_certificate;
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
                    _access_token = null;
                    _profile_list = null;
                    _client_certificate = null;

                    _base = value;
                    RaisePropertyChanged();
                }
            }
        }
        private Uri _base;

        /// <summary>
        /// Instance name to display in GUI
        /// </summary>
        public string DisplayName
        {
            get { return _display_name; }
            set { if (value != _display_name) { _display_name = value; RaisePropertyChanged(); } }
        }
        private string _display_name;

        /// <summary>
        /// Instance logo URI
        /// </summary>
        public Uri Logo
        {
            get { return _logo; }
            set { if (value != _logo) { _logo = value; RaisePropertyChanged(); } }
        }
        private Uri _logo;

        /// <summary>
        /// Request authorization event
        /// </summary>
        public event EventHandler<RequestAuthorizationEventArgs> RequestAuthorization;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance info
        /// </summary>
        public InstanceInfo() :
            base()
        {
        }

        /// <summary>
        /// Constructs a custom instance info
        /// </summary>
        /// <param name="_base"></param>
        public InstanceInfo(Uri _base)
        {
            // Set base.
            Base = _base;

            // Set display name to base URI hostname.
            DisplayName = _base.Host;

            // Set instance logo to /favicon.ico, perhaps we might get lucky.
            Logo = new UriBuilder(_base) { Path = "/favicon.ico" }.Uri;
        }

        /// <summary>
        /// Constructs the authenticating instance info for given federated instance source
        /// </summary>
        /// <param name="instance_source">Federated instance source</param>
        public InstanceInfo(FederatedInstanceSourceInfo instance_source) :
            this()
        {
            // Set display name to authorization URI hostname.
            DisplayName = instance_source.AuthorizationEndpoint.Host;

            // Set instance logo to /favicon.ico, perhaps we might get lucky.
            Logo = new UriBuilder(instance_source.AuthorizationEndpoint) { Path = "/favicon.ico" }.Uri;

            // Set API endpoints manually.
            _endpoints = new InstanceEndpoints()
            {
                AuthorizationEndpoint = instance_source.AuthorizationEndpoint,
                TokenEndpoint = instance_source.TokenEndpoint
            };
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return DisplayName;
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
                        endpoints.LoadJSON(JSON.Response.Get(
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
        /// Gets (and refreshes) access token, but does not initiate authorization when the token is not available silently returning <c>null</c>
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Access token or <c>null</c> if not available</returns>
        public AccessToken PeekAccessToken(CancellationToken ct = default(CancellationToken))
        {
            lock (_access_token_lock)
            {
                if (_access_token == null)
                {
                    // Get API endpoints.
                    var api = GetEndpoints(ct);

                    try
                    {
                        // Try to restore the access token from the settings.
                        var access_token = AccessToken.FromBase64String(Properties.Settings.Default.AccessTokens[api.AuthorizationEndpoint.AbsoluteUri]);

                        if (access_token.Expires.HasValue && access_token.Expires.Value <= DateTime.Now)
                        {
                            // Token expired. Refresh it.
                            access_token = access_token.RefreshToken(api.TokenEndpoint, null, ct);
                            if (access_token != null)
                            {
                                // If we got here, save the token.
                                _access_token = access_token;

                                // Update access token in the settings.
                                Properties.Settings.Default.AccessTokens[api.AuthorizationEndpoint.AbsoluteUri] = _access_token.ToBase64String();
                            }
                        }
                        else
                        {
                            // If we got here, save the token.
                            _access_token = access_token;
                        }
                    }
                    catch (Exception) { }
                }

                return _access_token;
            }
        }

        /// <summary>
        /// Gets (and refreshes) access token
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Access token or <c>null</c> if not available</returns>
        public AccessToken GetAccessToken(CancellationToken ct = default(CancellationToken))
        {
            lock (_access_token_lock)
            {
                PeekAccessToken(ct);

                if (_access_token == null)
                {
                    try
                    {
                        // If we got here, there was no saved token, or something is wrong with it: decoding failed, refreshing failed...
                        // Request user authorization.
                        var e = new RequestAuthorizationEventArgs();
                        RequestAuthorization?.Invoke(this, e);

                        if (e.AccessToken == null)
                            throw new AccessTokenNullException();

                        // If we got here, save the token.
                        _access_token = e.AccessToken;

                        // Save the access token to the settings.
                        Properties.Settings.Default.AccessTokens[GetEndpoints(ct).AuthorizationEndpoint.AbsoluteUri] = _access_token.ToBase64String();
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorAuthorization, ex); }
                }

                return _access_token;
            }
        }

        /// <summary>
        /// Resets access token
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        public void RefreshOrResetAccessToken(CancellationToken ct = default(CancellationToken))
        {
            lock (_access_token_lock)
            {
                // Get API endpoints.
                var api = GetEndpoints(ct);

                if (_access_token != null && _access_token.Expires.HasValue)
                {
                    try
                    {
                        // We have an expirable access token. Try to refresh it first.
                        var access_token = _access_token.RefreshToken(api.TokenEndpoint, null, ct);
                        if (access_token != null)
                        {
                            // If we got here, save the token.
                            _access_token = access_token;

                            // Save the access token to the settings.
                            Properties.Settings.Default.AccessTokens[api.AuthorizationEndpoint.AbsoluteUri] = _access_token.ToBase64String();

                            return;
                        }
                    }
                    catch (Exception) { }
                }

                // Remove access token from our internal state and from settings.
                _access_token = null;
                Properties.Settings.Default.AccessTokens.Remove(api.AuthorizationEndpoint.AbsoluteUri);
            }
        }

        /// <summary>
        /// Gets instance profile list available to the user
        /// </summary>
        /// <param name="authenticating_instance">Authenticating instance (can be same as this instance)</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Profile list</returns>
        public JSON.Collection<Models.ProfileInfo> GetProfileList(InstanceInfo authenticating_instance, CancellationToken ct = default(CancellationToken))
        {
            lock (_profile_list_lock)
            {
                if (_profile_list == null)
                {
                    retry:
                    try
                    {
                        // Get and load profile list.
                        var profile_list = new JSON.Collection<Models.ProfileInfo>();
                        profile_list.LoadJSONAPIResponse(JSON.Response.Get(
                            uri: GetEndpoints(ct).ProfileList,
                            token: authenticating_instance.GetAccessToken(ct),
                            ct: ct).Value, "profile_list", ct);

                        // If we got here, save the profile.
                        _profile_list = profile_list;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (AggregateException ex)
                    {
                        if (ex.InnerException is WebException ex_inner && ex_inner.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            // Access token was rejected (401 Unauthorized): reset access token and retry.
                            authenticating_instance.RefreshOrResetAccessToken(ct);
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
        /// Gets instance user info
        /// </summary>
        /// <param name="authenticating_instance">Authenticating instance (can be same as this instance)</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>User info</returns>
        public UserInfo GetUserInfo(InstanceInfo authenticating_instance, CancellationToken ct = default(CancellationToken))
        {
            // Get API endpoints.
            var api = GetEndpoints(ct);
            if (api.UserInfo == null)
                return null;

            retry:
            try
            {
                // Get and load user info.
                var user_info = new UserInfo();
                user_info.LoadJSONAPIResponse(JSON.Response.Get(
                    uri: api.UserInfo,
                    token: authenticating_instance.GetAccessToken(ct),
                    ct: ct).Value, "user_info", ct);
                return user_info;
            }
            catch (OperationCanceledException) { throw; }
            catch (AggregateException ex)
            {
                if (ex.InnerException is WebException ex_inner && ex_inner.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Access token was rejected (401 Unauthorized): reset access token and retry.
                    authenticating_instance.RefreshOrResetAccessToken(ct);
                    goto retry;
                }
                else
                    throw new AggregateException(Resources.Strings.ErrorUserInfoLoad, ex);
            }
            catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorUserInfoLoad, ex); }
        }

        /// <summary>
        /// Gets client certificate
        /// </summary>
        /// <param name="authenticating_instance">Authenticating instance (can be same as this instance)</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Client certificate. Certificate (including the private key) is saved to user certificate store.</returns>
        public X509Certificate2 GetClientCertificate(InstanceInfo authenticating_instance, CancellationToken ct = default(CancellationToken))
        {
            lock (_client_certificate_lock)
            {
                if (_client_certificate == null)
                {
                    // Open eduVPN client certificate store.
                    var store = new X509Store("org.eduvpn.app", StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadWrite);
                    try
                    {
                        if (Properties.Settings.Default.InstanceSettings.TryGetValue(Base.AbsoluteUri, out var instance_settings) && instance_settings.ClientCertificateHash != null)
                        {
                            // Try to restore previously issued client certificate from the certificate store first.
                            foreach (var cert in store.Certificates)
                            {
                                if (DateTime.Now < cert.NotAfter && cert.HasPrivateKey)
                                {
                                    // Not expired && Has the private key.
                                    if (cert.GetCertHash().SequenceEqual(instance_settings.ClientCertificateHash))
                                    {
                                        // Certificate found.
                                        _client_certificate = cert;
                                    }
                                }
                                else
                                {
                                    // Certificate expired or matching private key not found == Useless. Clean it from the store.
                                    store.Remove(cert);
                                }
                            }
                        }

                        if (_client_certificate == null)
                        {
                            retry:
                            try
                            {
                                // Get certificate and import it to Windows user certificate store.
                                var cert = new Models.Certificate();
                                cert.LoadJSONAPIResponse(JSON.Response.Get(
                                    uri: GetEndpoints(ct).CreateCertificate,
                                    param: new NameValueCollection
                                    {
                                        { "display_name", Resources.Strings.CertificateTitle }
                                    },
                                    token: authenticating_instance.GetAccessToken(ct),
                                    ct: ct).Value, "create_keypair", ct);
                                store.Add(cert.Value);

                                // If we got here, save the certificate.
                                _client_certificate = cert.Value;

                                // Save the certificate hash to the settings.
                                if (instance_settings == null)
                                    Properties.Settings.Default.InstanceSettings[Base.AbsoluteUri] = instance_settings = new Models.InstanceSettings() { ClientCertificateHash = _client_certificate.GetCertHash() };
                                else
                                    Properties.Settings.Default.InstanceSettings[Base.AbsoluteUri].ClientCertificateHash = _client_certificate.GetCertHash();
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (AggregateException ex)
                            {
                                if (ex.InnerException is WebException ex_inner && ex_inner.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Unauthorized)
                                {
                                    // Access token was rejected (401 Unauthorized): reset access token and retry.
                                    authenticating_instance.RefreshOrResetAccessToken(ct);
                                    goto retry;
                                }
                                else
                                    throw new AggregateException(Resources.Strings.ErrorClientCertificateLoad, ex);
                            }
                            catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorClientCertificateLoad, ex); }
                        }
                    }
                    finally { store.Close(); }
                }

                return _client_certificate;
            }
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
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(Dictionary<string, object>), obj.GetType());
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

            Base = (v = reader.GetAttribute("Base")) != null ? new Uri(v) : null;
            DisplayName = reader.GetAttribute("DisplayName");
            Logo = (v = reader.GetAttribute("Logo")) != null ? new Uri(v) : null;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("Base", Base.AbsoluteUri);
            if (DisplayName != null)
                writer.WriteAttributeString("DisplayName", DisplayName);
            if (Logo != null)
                writer.WriteAttributeString("Logo", Logo.AbsoluteUri);
        }

        #endregion
    }
}

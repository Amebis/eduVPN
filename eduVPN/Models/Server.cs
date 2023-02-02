/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduEx.System.Net;
using eduOAuth;
using eduVPN.JSON;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN server
    /// </summary>
    public class Server : ILoadableItem, IDisposable
    {
        #region Fields

        /// <summary>
        /// Server API endpoints
        /// </summary>
        private ServerEndpoints Endpoints;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object EndpointsLock = new object();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DateTimeOffset EndpointsExpires;

        /// <summary>
        /// Ed25519 keypair
        /// </summary>
        private eduLibsodium.Box Keypair;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object KeypairLock = new object();

        #endregion

        #region Properties

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
        /// <remarks>Sender is the authenticating server <see cref="Server"/>.</remarks>
        public event EventHandler<RequestAuthorizationEventArgs> RequestAuthorization;

        /// <summary>
        /// Called when a profile requests user authorization
        /// </summary>
        /// <param name="authenticatingServer">Authenticating server</param>
        /// <param name="e"><see cref="RequestAuthorization"/> event arguments</param>
        public void OnRequestAuthorization(Server authenticatingServer, RequestAuthorizationEventArgs e)
        {
            RequestAuthorization?.Invoke(authenticatingServer, e);

            if (e.AccessToken is InvalidToken)
                throw new InvalidAccessTokenException(string.Format(Resources.Strings.ErrorInvalidAccessToken, this));
        }

        /// <summary>
        /// Forget authorization event
        /// </summary>
        /// <remarks>Sender is the authenticating server <see cref="Server"/>.</remarks>
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
                if (Endpoints != null && DateTimeOffset.Now < EndpointsExpires)
                    return Endpoints;

                try
                {
                    // Get and load API endpoints.
                    var endpoints = new ServerEndpoints();
                    var uriBuilder = new UriBuilder(Base);
                    uriBuilder.Path += ".well-known/vpn-user-portal";
                    Trace.TraceInformation("Loading endpoints {0}", uriBuilder.Uri);
                    endpoints.LoadJSON(Xml.Response.Get(
                        uri: uriBuilder.Uri,
                        ct: ct).Value, ct);
                    Endpoints = endpoints;
                    EndpointsExpires = DateTimeOffset.Now.AddSeconds(60);
                    return endpoints;
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
            // Get API endpoints.
            var api = GetEndpoints(ct);
            var e = new RequestAuthorizationEventArgs("config");

            retry:
            // Request authentication token.
            OnRequestAuthorization(authenticatingServer, e);

            try
            {
                // Parse JSON string and get inner key/value dictionary.
                Trace.TraceInformation("Loading info {0}", api.Info);
                var obj = eduJSON.Parser.GetValue<Dictionary<string, object>>(
                    (Dictionary<string, object>)eduJSON.Parser.Parse(Xml.Response.Get(
                        uri: api.Info,
                        token: e.AccessToken,
                        ct: ct).Value, ct),
                    "info");

                // Load collection.
                if (!eduJSON.Parser.GetValue(obj, "profile_list", out List<object> obj2))
                    throw new eduJSON.InvalidParameterTypeException(nameof(obj2), typeof(Dictionary<string, object>), obj.GetType());

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
                return profileList;
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
                        // Or the token was revoked on the server side.
                        // Retry with forced access token refresh.
                        e.SourcePolicy = RequestAuthorizationEventArgs.SourcePolicyType.ForceAuthorization;
                        goto retry;
                    }
                }
                if (ex is WebExceptionEx exEx && ex.Response.ContentType == "application/json")
                {
                    var obj = (Dictionary<string, object>)eduJSON.Parser.Parse(exEx.ResponseText, ct);
                    throw new AggregateException(Resources.Strings.ErrorProfileListLoad + "\n" + eduJSON.Parser.GetValue<string>(obj, "error"), ex);
                }
                throw new AggregateException(Resources.Strings.ErrorProfileListLoad, ex);
            }
            catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorProfileListLoad, ex); }
        }

        /// <summary>
        /// Notifies server to release resources related to our sessions
        /// </summary>
        /// <param name="authenticatingServer">Authenticating server (can be same as this server)</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        public void Disconnect(Server authenticatingServer, CancellationToken ct = default)
        {
            var api = GetEndpoints(ct);
            var e = new RequestAuthorizationEventArgs("config");
            RequestAuthorization?.Invoke(authenticatingServer, e);
            try
            {
                Trace.TraceInformation("Disconnecting {0}", api.Disconnect);
                Xml.Response.Get(
                    uri: api.Disconnect,
                    param: new NameValueCollection(),
                    token: e.AccessToken,
                    ct: ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (WebException) { }
        }

        /// <summary>
        /// Removes server data from cache
        /// </summary>
        public void Forget()
        {
            // Ask authorization provider to forget our authorization token.
            ForgetAuthorization?.Invoke(this, new ForgetAuthorizationEventArgs("config"));
        }

        /// <summary>
        /// Returns Ed25519 public key
        /// </summary>
        /// <returns>Ed25519 public key</returns>
        public byte[] GetPublicKey()
        {
            lock (KeypairLock)
            {
                if (Keypair == null)
                    Keypair = new eduLibsodium.Box();
            }
            return Keypair.PublicKey;
        }

        /// <summary>
        /// Returns Ed25519 private key
        /// </summary>
        /// <returns>Ed25519 private key</returns>
        public byte[] GetPrivateKey()
        {
            lock (KeypairLock)
            {
                if (Keypair == null)
                    Keypair = new eduLibsodium.Box();
            }
            return Keypair.SecretKey;
        }

        /// <summary>
        /// Resets Ed25519 keypair
        /// </summary>
        public void ResetKeypair()
        {
            lock (KeypairLock)
            {
                Keypair = null;
            }
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
            EndpointsExpires = DateTimeOffset.MinValue;
            Base = new Uri(eduJSON.Parser.GetValue<string>(obj2, "base_url"));

            // Set support contact URLs.
            SupportContacts.Clear();
            if (eduJSON.Parser.GetValue(obj2, "support_contact", out List<object> support_contact))
                foreach (var c in support_contact)
                    if (c is string cStr)
                        SupportContacts.Add(new Uri(cStr));
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
                    if (Keypair != null)
                        Keypair.Dispose();
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

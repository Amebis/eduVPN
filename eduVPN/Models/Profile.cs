/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN profile
    /// </summary>
    public class Profile : JSON.ILoadableItem
    {
        #region Properties

        /// <summary>
        /// The server this profile belongs to
        /// </summary>
        public Server Server { get; }

        /// <summary>
        /// Profile identifier
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Profile name to display in GUI
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// Does this profile route all traffic thru the tunnel?
        /// </summary>
        public bool DefaultGateway { get; private set; }

        /// <summary>
        /// List of supported VPN protocols
        /// </summary>
        public HashSet<VPNProtocol> SupportedProtocols { get; private set; }

        /// <summary>
        /// Request authorization event
        /// </summary>
        /// <remarks>Sender is the profile <see cref="Profile"/>.</remarks>
        public event EventHandler<RequestAuthorizationEventArgs> RequestAuthorization;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a profile
        /// </summary>
        /// <param name="server">The server this profile belongs to</param>
        public Profile(Server server)
        {
            Server = server;
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return DisplayName ?? Id;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as Profile;
            if (!Server.Equals(other.Server) ||
                !Id.Equals(other.Id))
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return
                Server.GetHashCode() ^ Id.GetHashCode();
        }

        /// <summary>
        /// Gets configuration for the profile to connect
        /// </summary>
        /// <param name="authenticatingServer">Authenticating server (can be same as this server)</param>
        /// <param name="forceRefresh">Force client reauthorization</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Profile configuration</returns>
        public Xml.Response Connect(Server authenticatingServer, bool forceRefresh = false, string responseType = "application/x-openvpn-profile, application/x-wireguard-profile", CancellationToken ct = default)
        {
            // Get API endpoints.
            var api = Server.GetEndpoints(ct);
            var e = new RequestAuthorizationEventArgs("config");
            if (forceRefresh)
                e.SourcePolicy = RequestAuthorizationEventArgs.SourcePolicyType.ForceAuthorization;

            retry:
            // Request authentication token.
            RequestAuthorization?.Invoke(authenticatingServer, e);

            try
            {
                // Get complete profile config.
                Trace.TraceInformation("Connecting {0}", api.Connect);
                var profile = Xml.Response.Get(
                    uri: api.Connect,
                    param: new NameValueCollection {
                        { "profile_id", Id },
                        { "public_key", Convert.ToBase64String(Server.GetPublicKey()) },
                        { "prefer_tcp", Properties.Settings.Default.OpenVPNPreferTCP ? "yes" : "no" }
                    },
                    token: e.AccessToken,
                    responseType: responseType,
                    ct: ct);
                if (profile.ContentType.ToLowerInvariant() == "application/x-wireguard-profile")
                    profile.Value = profile.Value.Replace("[Interface]", "[Interface]\nPrivateKey = " + Convert.ToBase64String(Server.GetPrivateKey())); // SECURITY: Securely convert profile string to a SecureString
                return profile;
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
                    throw new AggregateException(Resources.Strings.ErrorProfileConfigLoad + "\n" + eduJSON.Parser.GetValue<string>(obj, "error"), ex);
                }
                throw new AggregateException(Resources.Strings.ErrorProfileConfigLoad, ex);
            }
            catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorProfileConfigLoad, ex); }
        }

        #endregion

        #region ILoadableItem Support

        /// <summary>
        /// Loads profile from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>display_name</c> and <c>profile_id</c> elements. <c>profile_id</c> is required. <c>display_name</c> and <c>profile_id</c> elements should be strings.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public void Load(object obj)
        {
            if (!(obj is Dictionary<string, object> obj2))
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());

            // Set identifier.
            Id = eduJSON.Parser.GetValue<string>(obj2, "profile_id");

            // Set display name.
            var displayName = new Dictionary<string, string>();
            DisplayName = eduJSON.Parser.GetDictionary(obj2, "display_name", displayName) ? displayName.GetLocalized(Id) : Id;

            DefaultGateway = eduJSON.Parser.GetValue(obj2, "default_gateway", out bool defaultGateway) && defaultGateway;

            // Set supported protocols.
            if (eduJSON.Parser.GetValue(obj2, "vpn_proto_list", out List<object> vpnProtoList))
            {
                SupportedProtocols = new HashSet<VPNProtocol>();
                foreach (var e in vpnProtoList)
                    if (e is string str)
                        switch (str)
                        {
                            case "openvpn": SupportedProtocols.Add(VPNProtocol.OpenVPN); break;
                            case "wireguard": SupportedProtocols.Add(VPNProtocol.WireGuard); break;
                        }
            }
            else
                SupportedProtocols = null;
        }

        #endregion
    }
}

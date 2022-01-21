/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Web;

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
        /// Request authorization event
        /// </summary>
        /// <remarks>Sender is the profile <see cref="eduVPN.Models.Profile"/>.</remarks>
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
        /// Gets profile OpenVPN configuration
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Profile configuration</returns>
        public string GetOpenVPNConfig(CancellationToken ct = default)
        {
            // Get API endpoints.
            var api = Server.GetEndpoints(ct);
            var e = new RequestAuthorizationEventArgs("config");

            retry:
            // Request authentication token.
            RequestAuthorization?.Invoke(this, e);

            try
            {
                // Get profile config.
                var uriBuilder = new UriBuilder(api.ProfileConfig);
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                query["profile_id"] = Id;
                uriBuilder.Query = query.ToString();
                var openVPNConfig = Xml.Response.Get(
                    uri: uriBuilder.Uri,
                    token: e.AccessToken,
                    responseType: "application/x-openvpn-profile",
                    ct: ct).Value;

                // If we got here, return the config.
                return openVPNConfig;
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
        }

        #endregion
    }
}

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Web;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN profile
    /// </summary>
    public class Profile : BindableBase, JSON.ILoadableItem
    {
        #region Properties

        /// <summary>
        /// The instance this profile belongs to
        /// </summary>
        public Instance Instance
        {
            get { return _instance; }
            set { SetProperty(ref _instance, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Instance _instance;

        /// <summary>
        /// Profile ID
        /// </summary>
        public string ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _id;

        /// <summary>
        /// Is profile available?
        /// </summary>
        public bool IsAvailable
        {
            get { return _is_available; }
            set { SetProperty(ref _is_available, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _is_available;

        /// <summary>
        /// Profile name to display in GUI
        /// </summary>
        public string DisplayName
        {
            get { return _display_name; }
            set { SetProperty(ref _display_name, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _display_name;

        /// <summary>
        /// Is 2-Factor authentication enabled for this profile?
        /// </summary>
        public bool IsTwoFactorAuthentication
        {
            get { return _is_two_factor_authentication;  }
            set { SetProperty(ref _is_two_factor_authentication, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _is_two_factor_authentication;

        /// <summary>
        /// Supported 2-Factor authentication methods
        /// </summary>
        public TwoFactorAuthenticationMethods TwoFactorMethods
        {
            get { return _two_factor_methods; }
            set { SetProperty(ref _two_factor_methods, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TwoFactorAuthenticationMethods _two_factor_methods;

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
        /// <remarks>Sender is the profile <see cref="eduVPN.Models.Profile"/>.</remarks>
        public event EventHandler<RequestAuthorizationEventArgs> RequestAuthorization;

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return DisplayName ?? ID;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as Profile;
            if (!Instance.Equals(other.Instance) ||
                !ID.Equals(other.ID))
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return
                Instance.GetHashCode() ^ ID.GetHashCode();
        }

        /// <summary>
        /// Gets profile OpenVPN configuration
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Profile configuration</returns>
        public string GetOpenVPNConfig(CancellationToken ct = default(CancellationToken))
        {
            // Get API endpoints.
            var api = Instance.GetEndpoints(ct);
            var e = new RequestAuthorizationEventArgs("config");

            retry:
            // Request authentication token.
            RequestAuthorization?.Invoke(this, e);

            try
            {
                // Get profile config.
                var uri_builder = new UriBuilder(api.ProfileConfig);
                var query = HttpUtility.ParseQueryString(uri_builder.Query);
                query["profile_id"] = ID;
                uri_builder.Query = query.ToString();
                var openvpn_config = Xml.Response.Get(
                    uri: uri_builder.Uri,
                    token: e.AccessToken,
                    response_type: "application/x-openvpn-profile",
                    ct: ct).Value;

                // If we got here, return the config.
                return openvpn_config;
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
                    throw new AggregateException(Resources.Strings.ErrorProfileConfigLoad, ex);
            }
            catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorProfileConfigLoad, ex); }
        }

        /// <summary>
        /// Gets profile OpenVPN complete configuration
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Profile configuration</returns>
        public string GetCompleteOpenVPNConfig(CancellationToken ct = default(CancellationToken))
        {
            // Get API endpoints.
            var api = Instance.GetEndpoints(ct);
            var e = new RequestAuthorizationEventArgs("config");

            retry:
            // Request authentication token.
            RequestAuthorization?.Invoke(this, e);

            try
            {
                // Get complete profile config.
                var openvpn_complete_config = Xml.Response.Get(
                    uri: api.ProfileCompleteConfig,
                    param: new NameValueCollection
                    {
                        { "display_name", String.Format("{0} Client for Windows", Properties.Settings.Default.ClientTitle) }, // Always use English display_name
                        { "profile_id", ID }
                    },
                    token: e.AccessToken,
                    response_type: "application/x-openvpn-profile",
                    ct: ct).Value;

                // If we got here, return the config.
                return openvpn_complete_config;
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
                    throw new AggregateException(Resources.Strings.ErrorProfileConfigLoad, ex);
            }
            catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorProfileConfigLoad, ex); }
        }

        #endregion

        #region ILoadableItem Support

        /// <summary>
        /// Loads profile from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>display_name</c>, <c>profile_id</c> and <c>two_factor</c> elements. <c>profile_id</c> is required. <c>display_name</c> and <c>profile_id</c> elements should be strings; <c>two_factor</c> should be boolean.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public void Load(object obj)
        {
            if (obj is Dictionary<string, object> obj2)
            {
                // Set ID.
                ID = eduJSON.Parser.GetValue<string>(obj2, "profile_id");

                // Set display name.
                DisplayName = eduJSON.Parser.GetLocalizedValue(obj2, "display_name", out string display_name) ? display_name : ID;

                // Set two-factor authentication.
                IsTwoFactorAuthentication = eduJSON.Parser.GetValue(obj2, "two_factor", out bool two_factor) ? two_factor : false;
                if (IsTwoFactorAuthentication)
                {
                    if (eduJSON.Parser.GetValue(obj2, "two_factor_method", out List<object> two_factor_method))
                    {
                        TwoFactorMethods = TwoFactorAuthenticationMethods.None;
                        foreach (var method in two_factor_method)
                            if (method is string method_str)
                                switch (method_str)
                                {
                                    case "totp": TwoFactorMethods |= TwoFactorAuthenticationMethods.TOTP; break;
                                    case "yubi": TwoFactorMethods |= TwoFactorAuthenticationMethods.YubiKey; break;
                                }
                    }
                    else
                        TwoFactorMethods = TwoFactorAuthenticationMethods.Any;
                }
                else
                    TwoFactorMethods = TwoFactorAuthenticationMethods.None;

                // Mark profile as available.
                IsAvailable = true;
            }
            else
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());
        }

        #endregion
    }
}

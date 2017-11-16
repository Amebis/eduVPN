/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        #region Fields

        /// <summary>
        /// Profile OpenVPN configuration
        /// </summary>
        private string _openvpn_config;
        private object _openvpn_config_lock = new object();

        /// <summary>
        /// Profile complete OpenVPN configuration
        /// </summary>
        private string _openvpn_complete_config;
        private object _openvpn_complete_config_lock = new object();

        #endregion

        #region Properties

        /// <summary>
        /// Profile ID
        /// </summary>
        public string ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }
        private string _id;

        /// <summary>
        /// Profile name to display in GUI
        /// </summary>
        public string DisplayName
        {
            get { return _display_name; }
            set { SetProperty(ref _display_name, value); }
        }
        private string _display_name;

        /// <summary>
        /// Is 2-Factor authentication enabled for this profile?
        /// </summary>
        public bool IsTwoFactorAuthentication
        {
            get { return _is_two_factor_authentication;  }
            set { SetProperty(ref _is_two_factor_authentication, value); }
        }
        private bool _is_two_factor_authentication;

        /// <summary>
        /// Supported 2-Factor authentication methods
        /// </summary>
        public TwoFactorAuthenticationMethods TwoFactorMethods
        {
            get { return _two_factor_methods; }
            set { SetProperty(ref _two_factor_methods, value); }
        }
        private TwoFactorAuthenticationMethods _two_factor_methods;

        /// <summary>
        /// Popularity factor in the [0.0, 1.0] range (default 1.0)
        /// </summary>
        public float Popularity
        {
            get { return _popularity; }
            set { SetProperty(ref _popularity, value); }
        }
        private float _popularity = 1.0f;

        /// <summary>
        /// Request authorization event
        /// </summary>
        public event EventHandler<RequestAuthorizationEventArgs> RequestAuthorization;

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

            var other = obj as Profile;
            if (!ID.Equals(other.ID))
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return
                ID.GetHashCode();
        }

        /// <summary>
        /// Gets profile OpenVPN configuration
        /// </summary>
        /// <param name="connecting_instance">Instance this profile is part of</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Profile configuration</returns>
        public string GetOpenVPNConfig(Instance connecting_instance, CancellationToken ct = default(CancellationToken))
        {
            lock (_openvpn_config_lock)
            {
                if (_openvpn_config == null)
                {
                    // Get API endpoints.
                    var api = connecting_instance.GetEndpoints(ct);
                    var e = new RequestAuthorizationEventArgs("config");

                    retry:
                    try
                    {
                        // Request authentication token.
                        RequestAuthorization?.Invoke(this, e);
                        if (e.AccessToken == null)
                            throw new AccessTokenNullException();

                        // Get profile config.
                        var uri_builder = new UriBuilder(api.ProfileConfig);
                        var query = HttpUtility.ParseQueryString(uri_builder.Query);
                        query["profile_id"] = ID;
                        uri_builder.Query = query.ToString();
                        var openvpn_config = JSON.Response.Get(
                            uri: uri_builder.Uri,
                            token: e.AccessToken,
                            response_type: "application/x-openvpn-profile",
                            ct: ct).Value;

                        // If we got here, save the config.
                        _openvpn_config = openvpn_config;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (WebException ex)
                    {
                        if (ex.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            // Access token was rejected (401 Unauthorized): retry with forced authorization, ignoring saved access token.
                            e.SourcePolicy = RequestAuthorizationEventArgs.SourcePolicyType.ForceAuthorization;
                            goto retry;
                        }
                        else
                            throw new AggregateException(Resources.Strings.ErrorProfileConfigLoad, ex);
                    }
                    catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorProfileConfigLoad, ex); }
                }

                return _openvpn_config;
            }
        }

        /// <summary>
        /// Gets profile OpenVPN complete configuration
        /// </summary>
        /// <param name="connecting_instance">Instance this profile is part of</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Profile configuration</returns>
        public string GetCompleteOpenVPNConfig(Instance connecting_instance, CancellationToken ct = default(CancellationToken))
        {
            lock (_openvpn_complete_config_lock)
            {
                if (_openvpn_complete_config == null)
                {
                    // Get API endpoints.
                    var api = connecting_instance.GetEndpoints(ct);
                    var e = new RequestAuthorizationEventArgs("config");

                    retry:
                    try
                    {
                        // Request authentication token.
                        RequestAuthorization?.Invoke(this, e);
                        if (e.AccessToken == null)
                            throw new AccessTokenNullException();

                        // Get complete profile config.
                        var openvpn_complete_config = JSON.Response.Get(
                            uri: api.ProfileCompleteConfig,
                            param: new NameValueCollection
                            {
                                { "display_name", String.Format(Resources.Strings.ProfileTitle, Environment.MachineName) },
                                { "profile_id", ID }
                            },
                            token: e.AccessToken,
                            response_type: "application/x-openvpn-profile",
                            ct: ct).Value;

                        // If we got here, save the config.
                        _openvpn_complete_config = openvpn_complete_config;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (WebException ex)
                    {
                        if (ex.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            // Access token was rejected (401 Unauthorized): retry with forced authorization, ignoring saved access token.
                            e.SourcePolicy = RequestAuthorizationEventArgs.SourcePolicyType.ForceAuthorization;
                            goto retry;
                        }
                        else
                            throw new AggregateException(Resources.Strings.ErrorProfileConfigLoad, ex);
                    }
                    catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorProfileConfigLoad, ex); }
                }

                return _openvpn_complete_config;
            }
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
            }
            else
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(Dictionary<string, object>), obj.GetType());
        }

        #endregion
    }
}

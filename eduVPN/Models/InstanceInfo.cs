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
using System.Threading;
using System.Threading.Tasks;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN instance (VPN service provider) basic info
    /// </summary>
    public class InstanceInfo : BindableBase, JSON.ILoadableItem
    {
        #region Fields

        private InstanceEndpoints _endpoints;

        #endregion

        #region Properties

        /// <summary>
        /// Instance base URI
        /// </summary>
        public Uri Base
        {
            get { return _base; }
            set { if (value != _base) { _base = value; RaisePropertyChanged(); } }
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
        /// Public key used for access token signing (<c>null</c> if none)
        /// </summary>
        public byte[] PublicKey
        {
            get { return _public_key; }
            set { _public_key = value; RaisePropertyChanged(); }
        }
        private byte[] _public_key;

        /// <summary>
        /// Is this instance manually entered by user?
        /// </summary>
        public bool IsCustom
        {
            get { return _is_custom; }
            set { if (value != _is_custom) { _is_custom = value; RaisePropertyChanged(); } }
        }
        private bool _is_custom;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs the instance info
        /// </summary>
        public InstanceInfo()
        { }

        /// <summary>
        /// Constructs the authenticating instance info for given federated instance list
        /// </summary>
        /// <param name="instance_list">Federated instance list</param>
        public InstanceInfo(InstanceInfoFederatedList instance_list)
        {
            // Assume same authenticating instance identity as instance list.
            DisplayName = instance_list.DisplayName;
            Logo = instance_list.Logo;

            // Set API endpoints manually.
            _endpoints = new InstanceEndpoints()
            {
                AuthorizationEndpoint = instance_list.AuthorizationEndpoint,
                TokenEndpoint = instance_list.TokenEndpoint
            };
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return DisplayName;
        }

        /// <summary>
        /// Gets and loads instance endpoints.
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Instance endpoints</returns>
        public InstanceEndpoints GetEndpoints(CancellationToken ct = default(CancellationToken))
        {
            var task = GetEndpointsAsync(ct);
            try
            {
                task.Wait(ct);
                return task.Result;
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Gets and loads instance endpoints asynchronously.
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Asynchronous operation with expected instance endpoints</returns>
        public async Task<InstanceEndpoints> GetEndpointsAsync(CancellationToken ct = default(CancellationToken))
        {
            if (_endpoints == null)
            {
                try
                {
                    // Get and load API endpoints.
                    _endpoints = new InstanceEndpoints();
                    var uri_builder = new UriBuilder(Base);
                    uri_builder.Path += "info.json";
                    _endpoints.LoadJSON((await JSON.Response.GetAsync(
                        uri: uri_builder.Uri,
                        ct: ct)).Value, ct);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorEndpointsLoad, ex); }
            }

            return _endpoints;
        }

        /// <summary>
        /// Gets (and refreshes) access token from settings
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Access token or <c>null</c> if not available</returns>
        public AccessToken GetAccessToken(CancellationToken ct = default(CancellationToken))
        {
            var task = GetAccessTokenAsync(ct);
            try
            {
                task.Wait(ct);
                return task.Result;
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Gets (and refreshes) access token from settings asynchronously
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Asynchronous operation with expected access token</returns>
        public async Task<AccessToken> GetAccessTokenAsync(CancellationToken ct = default(CancellationToken))
        {
            // Get API endpoints.
            var api = GetEndpointsAsync(ct);

            AccessToken token = null;
            try
            {
                // Try to restore the access token from the settings.
                var at = Properties.Settings.Default.AccessTokens[(await api).AuthorizationEndpoint.AbsoluteUri];
                if (at != null)
                    token = AccessToken.FromBase64String(at);
            }
            catch (Exception) { return null; }

            if (token != null && token.Expires.HasValue && token.Expires.Value <= DateTime.Now)
            {
                // The access token expired. Try refreshing it.
                try
                {
                    token = await token.RefreshTokenAsync(
                        (await api).TokenEndpoint,
                        null,
                        ct);
                }
                catch (Exception) { token = null; }
            }

            return token;
        }

        #endregion

        #region ILoadableItem Support

        /// <summary>
        /// Loads instance from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>base_uri</c>, <c>logo_uri</c> and <c>display_name</c> elements. <c>base_uri</c> is required. All elements should be strings.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public void Load(object obj)
        {
            if (obj is Dictionary<string, object> obj2)
            {
                // Set base URI.
                Base = new Uri(eduJSON.Parser.GetValue<string>(obj2, "base_uri"));

                // Set display name.
                DisplayName = eduJSON.Parser.GetValue(obj2, "display_name", out string display_name) ? display_name : Base.Host;

                // Set logo URI.
                Logo = eduJSON.Parser.GetValue(obj2, "logo_uri", out string logo_uri) ? new Uri(logo_uri) : null;

                // Set public key.
                PublicKey = eduJSON.Parser.GetValue(obj2, "public_key", out string pub_key) ? Convert.FromBase64String(pub_key) : null;
            }
            else
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(Dictionary<string, object>), obj.GetType());
        }

        #endregion
    }
}

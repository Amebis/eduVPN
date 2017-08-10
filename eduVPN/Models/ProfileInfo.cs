/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN profile info
    /// </summary>
    public class ProfileInfo : BindableBase, JSON.ILoadableItem
    {
        #region Fields

        /// <summary>
        /// Profile OpenVPN configuration
        /// </summary>
        private string _openvpn_config;

        #endregion

        #region Properties

        /// <summary>
        /// Profile ID
        /// </summary>
        public string ID
        {
            get { return _id; }
            set { if (value != _id) { _id = value; RaisePropertyChanged(); } }
        }
        private string _id;

        /// <summary>
        /// Profile name to display in GUI
        /// </summary>
        public string DisplayName
        {
            get { return _display_name; }
            set { if (value != _display_name) { _display_name = value; RaisePropertyChanged(); } }
        }
        private string _display_name;

        /// <summary>
        /// Is two-factor authentication enabled for this profile?
        /// </summary>
        public bool IsTwoFactorAuthentication
        {
            get { return _is_two_factor_authentication;  }
            set { if (value != _is_two_factor_authentication) { _is_two_factor_authentication = value; RaisePropertyChanged(); } }
        }
        private bool _is_two_factor_authentication;

        #endregion

        #region Methods

        public override string ToString()
        {
            return DisplayName;
        }

        /// <summary>
        /// Gets profile OpenVPN configuration
        /// </summary>
        /// <param name="instance">Instance this profile is part of</param>
        /// <param name="token">Access token</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Profile configuration</returns>
        public string GetOpenVPNConfig(InstanceInfo instance, AccessToken token, CancellationToken ct = default(CancellationToken))
        {
            var task = GetOpenVPNConfigAsync(instance, token, ct);
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
        /// Gets profile OpenVPN configuration asynchronously
        /// </summary>
        /// <param name="instance">Instance this profile is part of</param>
        /// <param name="token">Access token</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Asynchronous operation with expected profile configuration</returns>
        public async Task<string> GetOpenVPNConfigAsync(InstanceInfo instance, AccessToken token, CancellationToken ct = default(CancellationToken))
        {
            if (_openvpn_config == null)
            {
                // Get API endpoints.
                var api = await instance.GetEndpointsAsync(ct);

                try
                {
                    // Get profile config.
                    var uri_builder = new UriBuilder(api.ProfileConfig);
                    var query = HttpUtility.ParseQueryString(uri_builder.Query);
                    query["profile_id"] = ID;
                    uri_builder.Query = query.ToString();
                    _openvpn_config = (await JSON.Response.GetAsync(
                        uri: uri_builder.Uri,
                        token: token,
                        response_type: "application/x-openvpn-profile",
                        ct: ct)).Value;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorProfileConfigLoad, ex); }
            }

            return _openvpn_config;
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
                DisplayName = eduJSON.Parser.GetValue(obj2, "display_name", out string display_name) ? display_name : ID;

                // Set two-factor authentication.
                IsTwoFactorAuthentication = eduJSON.Parser.GetValue(obj2, "two_factor", out bool two_factor) ? two_factor : false;
            }
            else
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(Dictionary<string, object>), obj.GetType());
        }

        #endregion
    }
}

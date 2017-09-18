/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Web;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN profile info
    /// </summary>
    public class ProfileInfo : BindableBase, JSON.ILoadableItem, IXmlSerializable
    {
        #region Fields

        /// <summary>
        /// Profile OpenVPN configuration
        /// </summary>
        private string _openvpn_config;
        private object _openvpn_config_lock = new object();

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
        /// Is 2-Factor authentication enabled for this profile?
        /// </summary>
        public bool IsTwoFactorAuthentication
        {
            get { return _is_two_factor_authentication;  }
            set { if (value != _is_two_factor_authentication) { _is_two_factor_authentication = value; RaisePropertyChanged(); } }
        }
        private bool _is_two_factor_authentication;

        /// <summary>
        /// Supported 2-Factor authentication methods
        /// </summary>
        public TwoFactorAuthenticationMethods TwoFactorMethods
        {
            get { return _two_factor_methods; }
            set { if (value != _two_factor_methods) { _two_factor_methods = value; RaisePropertyChanged(); } }
        }
        private TwoFactorAuthenticationMethods _two_factor_methods;

        #endregion

        #region Methods

        public override string ToString()
        {
            return DisplayName;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as ProfileInfo;
            if (!ID.Equals(other.ID))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return
                ID.GetHashCode();
        }

        /// <summary>
        /// Gets profile OpenVPN configuration
        /// </summary>
        /// <param name="connecting_instance">Instance this profile is part of</param>
        /// <param name="authenticating_instance">Authenticating instance (can be same as this instance)</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Profile configuration</returns>
        public string GetOpenVPNConfig(InstanceInfo connecting_instance, InstanceInfo authenticating_instance, CancellationToken ct = default(CancellationToken))
        {
            lock (_openvpn_config_lock)
            {
                if (_openvpn_config == null)
                {
                    // Get API endpoints.
                    var api = connecting_instance.GetEndpoints(ct);

                    retry:
                    try
                    {
                        // Get profile config.
                        var uri_builder = new UriBuilder(api.ProfileConfig);
                        var query = HttpUtility.ParseQueryString(uri_builder.Query);
                        query["profile_id"] = ID;
                        uri_builder.Query = query.ToString();
                        var openvpn_config = JSON.Response.Get(
                            uri: uri_builder.Uri,
                            token: authenticating_instance.GetAccessToken(ct),
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
                            // Access token was rejected (401 Unauthorized): reset access token and retry.
                            authenticating_instance.RefreshOrResetAccessToken(ct);
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

        #region IXmlSerializable Support

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            string v;

            ID = (v = reader.GetAttribute("ID")) != null ? v : null;
            DisplayName = reader.GetAttribute("DisplayName");
            IsTwoFactorAuthentication = (v = reader.GetAttribute("IsTwoFactorAuthentication")) != null && bool.TryParse(v, out var v_bool) ? v_bool : false;
            TwoFactorMethods = (v = reader.GetAttribute("TwoFactorMethods")) != null && int.TryParse(v, out var v_int) ? (TwoFactorAuthenticationMethods)v_int : TwoFactorAuthenticationMethods.None;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("ID", ID);
            if (DisplayName != null)
                writer.WriteAttributeString("DisplayName", DisplayName);
            writer.WriteAttributeString("IsTwoFactorAuthentication", IsTwoFactorAuthentication ? "true" : "false");
            writer.WriteAttributeString("TwoFactorMethods", ((int)TwoFactorMethods).ToString());
        }

        #endregion
    }
}

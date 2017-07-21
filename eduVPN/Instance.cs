/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using Prism.Mvvm;
using System;
using System.Collections.Generic;

namespace eduVPN
{
    /// <summary>
    /// An eduVPN instance = VPN service provider
    /// </summary>
    public class Instance : BindableBase, JSON.ILoadableItem
    {
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
        /// Is this instance manually entered by user
        /// </summary>
        public bool IsCustom
        {
            get { return _is_custom; }
            set { if (value != _is_custom) { _is_custom = value; RaisePropertyChanged(); } }
        }
        private bool _is_custom;

        /// <summary>
        /// Instance API endpoints
        /// </summary>
        public API Endpoints
        {
            get { return _endpoints; }
            set { _endpoints = value; RaisePropertyChanged(); }
        }
        private API _endpoints;

        /// <summary>
        /// OAuth access token
        /// </summary>
        public AccessToken AccessToken
        {
            get { return _access_token; }
            set { _access_token = value; RaisePropertyChanged(); }
        }
        private AccessToken _access_token;

        #endregion

        #region Methods

        public override string ToString()
        {
            return DisplayName;
        }

        /// <summary>
        /// Loads instance from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>base_uri</c>, <c>logo_uri</c> and <c>display_name</c> elements. <c>base_uri</c> is required. All elements should be strings.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public void Load(object obj)
        {
            var obj2 = obj as Dictionary<string, object>;
            if (obj2 == null)
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(Dictionary<string, object>), obj.GetType());

            // Set base URI.
            Base = new Uri(eduJSON.Parser.GetValue<string>(obj2, "base_uri"));

            // Set display name.
            if (eduJSON.Parser.GetValue(obj2, "display_name", out string display_name))
                DisplayName = display_name;
            else
                DisplayName = Base.Host;

            // Set logo URI.
            if (eduJSON.Parser.GetValue(obj2, "logo_uri", out string logo_uri))
                Logo = new Uri(logo_uri);
        }

        #endregion
    }
}

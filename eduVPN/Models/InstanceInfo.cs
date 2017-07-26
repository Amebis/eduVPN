/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN instance (VPN service provider) basic info
    /// </summary>
    public class InstanceInfo : BindableBase, JSON.ILoadableItem
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

        #region Methods

        public override string ToString()
        {
            return DisplayName;
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
            var obj2 = obj as Dictionary<string, object>;
            if (obj2 == null)
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(Dictionary<string, object>), obj.GetType());

            // Set base URI.
            Base = new Uri(eduJSON.Parser.GetValue<string>(obj2, "base_uri"));

            // Set display name.
            DisplayName = eduJSON.Parser.GetValue(obj2, "display_name", out string display_name) ? display_name : Base.Host;

            // Set logo URI.
            Logo = eduJSON.Parser.GetValue(obj2, "logo_uri", out string logo_uri) ? new Uri(logo_uri) : null;

            // Set public key.
            PublicKey = eduJSON.Parser.GetValue(obj2, "public_key", out string pub_key) ? Convert.FromBase64String(pub_key) : null;
        }

        #endregion
    }
}

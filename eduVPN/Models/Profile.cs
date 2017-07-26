/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN profile
    /// </summary>
    public class Profile : BindableBase, JSON.ILoadableItem
    {
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
        /// Loads profile from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>display_name</c>, <c>profile_id</c> and <c>two_factor</c> elements. <c>profile_id</c> is required. <c>display_name</c> and <c>profile_id</c> elements should be strings; <c>two_factor</c> should be boolean.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public void Load(object obj)
        {
            var obj2 = obj as Dictionary<string, object>;
            if (obj2 == null)
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(Dictionary<string, object>), obj.GetType());

            // Set ID.
            ID = eduJSON.Parser.GetValue<string>(obj2, "profile_id");

            // Set display name.
            DisplayName = eduJSON.Parser.GetValue(obj2, "display_name", out string display_name) ? display_name : ID;

            // Set two-factor authentication.
            IsTwoFactorAuthentication = eduJSON.Parser.GetValue(obj2, "two_factor", out bool two_factor) ? two_factor : false;
        }

        #endregion
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;

namespace eduVPN
{
    /// <summary>
    /// An eduVPN instance = VPN service provider
    /// </summary>
    public class Instance
    {
        #region Properties

        /// <summary>
        /// Instance base URI
        /// </summary>
        public Uri BaseURI { get; }

        /// <summary>
        /// Instance name to display in GUI
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Instance logo URI
        /// </summary>
        public Uri LogoURI { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>base_uri</c>, <c>logo_uri</c> and <c>display_name</c> elements. <c>base_uri</c> is required. All elements should be strings.</param>
        public Instance(Dictionary<string, object> obj)
        {
            // Set base URI.
            BaseURI = new Uri(eduJSON.Parser.GetValue<string>(obj, "base_uri"));

            // Set display name.
            if (eduJSON.Parser.GetValue(obj, "display_name", out string display_name))
                DisplayName = display_name;
            else
                DisplayName = BaseURI.Host;

            // Set logo URI.
            if (eduJSON.Parser.GetValue(obj, "logo_uri", out string logo_uri))
                LogoURI = new Uri(logo_uri);
        }

        #endregion
    }
}

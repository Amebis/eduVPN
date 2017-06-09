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
        /// <summary>
        /// Constructs a new instance with given base URI
        /// </summary>
        /// <param name="base_uri">Instance base URI</param>
        public Instance(string base_uri)
        {
            BaseURI = new Uri(base_uri);
            DisplayName = BaseURI.Host; // Deduce display name from the base URL hostname.
        }

        /// <summary>
        /// Constructs a new instance with given base URI and display name
        /// </summary>
        /// <param name="base_uri">Instance base URI</param>
        /// <param name="display_name">Instance display name</param>
        public Instance(string base_uri, string display_name)
        {
            BaseURI = new Uri(base_uri);
            DisplayName = display_name;
        }

        /// <summary>
        /// Constructs a new instance from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with "base_uri", "logo_uri" and "display_name" elements. "base_uri" is required. All elements should be strings.</param>
        public Instance(Dictionary<string, object> obj)
        {
            // Set base URI.
            object base_uri;
            if (obj.TryGetValue("base_uri", out base_uri) && base_uri.GetType() == typeof(string))
                BaseURI = new Uri((string)base_uri);
            else
                throw new ArgumentException(String.Format(Resources.ErrorMissingDataValue, "base_uri"), "obj");

            // Set display name.
            object display_name;
            if (obj.TryGetValue("display_name", out display_name) && display_name.GetType() == typeof(string))
                DisplayName = (string)display_name;
            else
                DisplayName = BaseURI.Host;

            // Set logo URI.
            object logo_uri;
            if (obj.TryGetValue("logo_uri", out logo_uri) && logo_uri.GetType() == typeof(string))
                LogoURI = new Uri((string)logo_uri);
        }

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
    }
}

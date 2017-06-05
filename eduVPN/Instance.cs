/*
    Copyright 2017 Amebis

    This file is part of eduVPN.

    eduVPN is free software: you can redistribute it and/or modify it
    under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    eduVPN is distributed in the hope that it will be useful, but
    WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with eduVPN. If not, see <http://www.gnu.org/licenses/>.
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
            BaseURI = new System.Uri(base_uri);
            DisplayName = BaseURI.Host; // Deduce display name from the base URL hostname.
        }

        /// <summary>
        /// Constructs a new instance with given base URI and display name
        /// </summary>
        /// <param name="base_uri">Instance base URI</param>
        /// <param name="display_name">Instance display name</param>
        public Instance(string base_uri, string display_name)
        {
            BaseURI = new System.Uri(base_uri);
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
                BaseURI = new System.Uri((string)base_uri);
            else
                throw new ArgumentException(Resources.ErrorMissingBaseURI, "obj");

            // Set display name.
            object display_name;
            if (obj.TryGetValue("display_name", out display_name) && display_name.GetType() == typeof(string))
                DisplayName = (string)display_name;
            else
                DisplayName = BaseURI.Host;

            // Set logo URI.
            object logo_uri;
            if (obj.TryGetValue("logo_uri", out logo_uri) && logo_uri.GetType() == typeof(string))
                LogoURI = new System.Uri((string)logo_uri);
        }

        /// <summary>
        /// Instance base URI
        /// </summary>
        public System.Uri BaseURI { get; }

        /// <summary>
        /// Instance name to display in GUI
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Instance logo URI
        /// </summary>
        public System.Uri LogoURI { get; }
    }
}

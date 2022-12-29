/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// Secure internet server
    /// </summary>
    public class SecureInternetServer : Server
    {
        #region Properties

        /// <summary>
        /// Server country
        /// </summary>
        public Country Country { get; private set; }

        /// <summary>
        /// Server authentication URI template
        /// </summary>
        public string AuthenticationUriTemplate { get; private set; }

        /// <summary>
        /// Organization identifier
        /// </summary>
        public string OrganizationId { get; set; }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return Country != null ? Country.ToString() : Base.Host;
        }

        #endregion

        #region ILoadableItem Support

        /// <summary>
        /// Loads institute access server from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>display_name</c>, <c>keyword_list</c> and other elements.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public override void Load(object obj)
        {
            if (!(obj is Dictionary<string, object> obj2))
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());

            base.Load(obj);

            // Set authentication URI template.
            AuthenticationUriTemplate = eduJSON.Parser.GetValue(obj2, "authentication_url_template", out string authenticationUriTemplate) ? authenticationUriTemplate : null;

            // Set country.
            Country = eduJSON.Parser.GetValue(obj2, "country_code", out string countryCode) ? new Country(countryCode) : null;
        }

        #endregion
    }
}

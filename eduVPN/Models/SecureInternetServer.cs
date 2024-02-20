/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;
using System.Diagnostics;

namespace eduVPN.Models
{
    /// <summary>
    /// Secure internet server
    /// </summary>
    public class SecureInternetServer : DiscoverableServer
    {
        #region Properties

        /// <inheritdoc/>
        public override ServerType ServerType { get => ServerType.SecureInternet; }

        /// <summary>
        /// Currently configured location
        /// </summary>
        public Country Country
        {
            get => _Country;
            set => SetProperty(ref _Country, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Country _Country;

        /// <summary>
        /// List of available secure internet locations
        /// </summary>
        public HashSet<Country> Locations { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates secure internet server
        /// </summary>
        /// <param name="org">Organization</param>
        public SecureInternetServer(Organization org) : base(org.Id)
        { }

        /// <summary>
        /// Creates secure internet server
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>identifier</c>, <c>display_name</c>, <c>profiles</c>, <c>country_code</c>, <c>locations</c>, <c>delisted</c> elements.</param>
        public SecureInternetServer(Dictionary<string, object> obj) : base(obj)
        {
            Country = eduJSON.Parser.GetValue(obj, "country_code", out string countryCode) && countryCode != null ? new Country(countryCode) : null;
            if (eduJSON.Parser.GetValue(obj, "locations", out List<object> locations) && locations != null)
            {
                Locations = new HashSet<Country>();
                foreach (var e in locations)
                    if (e is string s)
                        Locations.Add(new Country(s));
            }
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return Country != null ? Country.ToString() : base.ToString();
        }

        #endregion
    }
}

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

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return Country != null ? Country.ToString() : base.ToString();
        }

        #endregion

        #region Utf8Json

        /// <summary>
        /// Creates secure internet server
        /// </summary>
        /// <param name="json">JSON object</param>
        public SecureInternetServer(Json json) : base(json)
        {
            Country = json.country_code != null ? new Country(json.country_code) : null;
            if (json.locations != null)
            {
                Locations = new HashSet<Country>();
                foreach (var e in json.locations)
                    Locations.Add(new Country(e));
            }
        }

        #endregion
    }
}

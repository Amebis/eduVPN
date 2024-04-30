/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduvpn-common ask Secure Internet location transition
    /// </summary>
    public class AskLocationTransition : RequiredAskTransition
    {
        #region Properties

        /// <summary>
        /// List of countries
        /// </summary>
        public HashSet<Country> Countries { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates transition
        /// </summary>
        /// <param name="json">JSON object</param>
        public AskLocationTransition(Json json) : base(json)
        {
            Countries = new HashSet<Country>();
            if (!(json.data is List<object> obj))
                throw new ArgumentException();
            foreach (var item in obj)
                if (item is string s)
                    Countries.Add(new Country(s));
        }

        #endregion
    }
}

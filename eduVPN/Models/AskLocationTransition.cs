/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Common;
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
        /// <param name="obj">Key/value dictionary with <c>cookie</c> and <c>data</c> elements. <c>data</c> is required.</param>
        public AskLocationTransition(IReadOnlyDictionary<string, object> obj) :
            base(obj)
        {
            Countries = new HashSet<Country>();
            foreach (var item in obj.GetValue<List<object>>("data"))
                if (item is string s)
                    Countries.Add(new Country(s));
        }

        #endregion
    }
}

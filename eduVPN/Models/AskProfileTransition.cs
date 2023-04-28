/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Common;
using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduvpn-common ask Secure Internet location transition
    /// </summary>
    public class AskProfileTransition : RequiredAskTransition
    {
        #region Properties

        /// <summary>
        /// List of countries
        /// </summary>
        public ProfileDictionary Profiles { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates transition
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>cookie</c> and <c>data</c> elements. <c>data</c> is required.</param>
        public AskProfileTransition(IReadOnlyDictionary<string, object> obj) :
            base(obj)
        {
            Profiles = new ProfileDictionary(obj.GetValue<Dictionary<string, object>>("data"));
        }

        #endregion
    }
}

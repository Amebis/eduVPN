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
    public class AskProfileTransition : RequiredAskTransition
    {
        #region Properties

        /// <summary>
        /// List of countries
        /// </summary>
        public ProfileDictionary Profiles { get; }

        #endregion

        #region Utf8Json

        /// <summary>
        /// Creates transition
        /// </summary>
        /// <param name="json">JSON object</param>
        public AskProfileTransition(Json json) : base(json)
        {
            if (!(json.data is Dictionary<string, object> obj))
                throw new ArgumentException();
            Profiles = new ProfileDictionary(obj);
        }

        #endregion
    }
}

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduvpn-common transition with question
    /// </summary>
    public class RequiredAskTransition
    {
        #region Properties

        /// <summary>
        /// Operation cookie handle
        /// </summary>
        public IntPtr Cookie { get; }

        #endregion

        #region Utf8Json

        public class Json
        {
            public long? cookie { get; set; }
            public object data { get; set; }
        }

        /// <summary>
        /// Creates transition
        /// </summary>
        /// <param name="json">JSON object</param>
        public RequiredAskTransition(Json json)
        {
            Cookie = json.cookie != null ? (IntPtr)json.cookie.Value : IntPtr.Zero;
        }

        #endregion
    }
}

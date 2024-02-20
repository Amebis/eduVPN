/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Common;
using System;
using System.Collections.Generic;

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

        #region Constructors

        /// <summary>
        /// Creates transition
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>cookie</c> and <c>data</c> elements. <c>data</c> is required.</param>
        public RequiredAskTransition(IReadOnlyDictionary<string, object> obj)
        {
            Cookie = obj.TryGetValue("cookie", out long data) ? (IntPtr)data : IntPtr.Zero;
        }

        #endregion
    }
}

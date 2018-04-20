/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;
using System.Diagnostics;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN certificate check result
    /// </summary>
    public class CertificateCheck : JSON.ILoadableItem
    {
        #region Properties

        /// <summary>
        /// Is certificate valid
        /// </summary>
        public bool IsValid { get => _is_valid; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _is_valid;

        #endregion

        #region ILoadableItem Support

        /// <summary>
        /// Loads client certificate check result from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>is_valid</c> element with boolean value.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public void Load(object obj)
        {
            if (obj is Dictionary<string, object> obj2)
            {
                // Set check result.
                _is_valid = eduJSON.Parser.GetValue<bool>(obj2, "is_valid");
            }
            else
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());
        }

        #endregion
    }
}

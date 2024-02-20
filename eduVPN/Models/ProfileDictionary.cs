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
    /// Dictionary of profiles
    /// </summary>

    public class ProfileDictionary : Dictionary<string, Profile>
    {
        #region Properties

        /// <summary>
        /// Current profile
        /// </summary>
        public Profile Current {
            get => TryGetValue(_Current, out var profile) ? profile : null;
            set { _Current = value?.Id; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _Current;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates server list
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>server_list</c> and other optional elements</param>
        public ProfileDictionary(Dictionary<string, object> obj)
        {
            _Current = eduJSON.Parser.GetValue(obj, "current", out string current) ? current : null;
            if (eduJSON.Parser.GetValue(obj, "map", out Dictionary<string, object> map) && map != null)
                foreach (var el in map)
                {
                    if (!(el.Value is Dictionary<string, object> obj3))
                        throw new eduJSON.InvalidParameterTypeException("map", typeof(Dictionary<string, object>), el.GetType());
                    Add(el.Key, new Profile(el.Key, obj3));
                }
        }

        #endregion
    }
}

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
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

        #region Utf8Json

        public class Json
        {
            public string current { get; set; }
            public Dictionary<string, Profile.Json> map { get; set; }
            public List<Profile.Json> profile_list { get; set; }
        }

        /// <summary>
        /// Creates organization list
        /// </summary>
        public ProfileDictionary(Dictionary<string, object> obj)
        {
            _Current = obj.TryGetValue("current", out var current) && current is string currentStr ? currentStr : null;
            if (obj.TryGetValue("map", out var map) && map is Dictionary<string, object> dict)
            {
                foreach (var el in dict)
                {
                    if (!(el.Value is Dictionary<string, object> obj2))
                        throw new ArgumentException();
                    Add(el.Key, new Profile(el.Key, obj2));
                }
            }
        }

        /// <summary>
        /// Creates organization list
        /// </summary>
        public ProfileDictionary(Json json)
        {
            _Current = json.current;
            if (json.map != null)
                foreach (var el in json.map)
                    Add(el.Key, new Profile(el.Key, el.Value));
        }

        #endregion
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;

namespace eduVPN
{
    /// <summary>
    /// An eduVPN list of profiles
    /// </summary>
    public class ProfileList : ObservableCollection<Profile>
    {
        #region Properties

        /// <summary>
        /// Is the profile list OK?
        /// </summary>
        public bool IsOK
        {
            get { return _is_ok; }
            set { if (value != _is_ok) { _is_ok = value; OnPropertyChanged(new PropertyChangedEventArgs("IsOK")); } }
        }
        private bool _is_ok;

        #endregion

        #region Methods

        /// <summary>
        /// Loads profile list from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>profile_list</c> and other optional elements.</param>
        public void Load(Dictionary<string, object> obj)
        {
            Clear();

            var profile_list = eduJSON.Parser.GetValue<Dictionary<string, object>>(obj, "profile_list");

            // Parse all profiles listed. Don't do it in parallel to preserve the sort order.
            foreach (var el in eduJSON.Parser.GetValue<List<object>>(profile_list, "data"))
            {
                if (el.GetType() == typeof(Dictionary<string, object>))
                {
                    var profile = new Profile();
                    profile.Load((Dictionary<string, object>)el);
                    Add(profile);
                }
            }

            // Parse OK flag.
            IsOK = eduJSON.Parser.GetValue<bool>(profile_list, "ok", out bool ok) ? ok : true;
        }

        /// <summary>
        /// Loads profile list from a JSON string
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <param name="ct">The token to monitor for cancellation requests.</param>
        public void Load(string json, CancellationToken ct = default(CancellationToken))
        {
            Load((Dictionary<string, object>)eduJSON.Parser.Parse(json, ct));
        }

        #endregion
    }
}

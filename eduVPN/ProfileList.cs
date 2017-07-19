/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace eduVPN
{
    /// <summary>
    /// An eduVPN list of profiles
    /// </summary>
    public class ProfileList : ObservableCollection<Profile>
    {
        #region Methods

        /// <summary>
        /// Loads profile list from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">List of key/value dictionaries with <c>profile_list</c> and other optional elements.</param>
        public void Load(List<object> obj)
        {
            Clear();

            // Parse all profiles listed. Don't do it in parallel to preserve the sort order.
            foreach (var el in obj)
            {
                if (el.GetType() == typeof(Dictionary<string, object>))
                {
                    var profile = new Profile();
                    profile.Load((Dictionary<string, object>)el);
                    Add(profile);
                }
            }
        }

        #endregion
    }
}

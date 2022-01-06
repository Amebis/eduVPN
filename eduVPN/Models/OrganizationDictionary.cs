/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// Dictionary of organizations
    /// </summary>
    public class OrganizationDictionary : Dictionary<string, Organization>, JSON.ILoadableItem
    {
        #region ILoadableItem Support

        /// <summary>
        /// Loads organization list from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>organization_list</c> and other optional elements</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public virtual void Load(object obj)
        {
            if (!(obj is Dictionary<string, object> obj2))
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());

            // Parse all servers listed.
            Clear();
            foreach (var el in eduJSON.Parser.GetValue<List<object>>(obj2, "organization_list"))
            {
                var entry = new Organization();
                entry.Load(el);
                Add(entry.Id, entry);
            }
        }

        #endregion
    }
}

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// Dictionary of organizations
    /// </summary>
    public class OrganizationDictionary : Dictionary<string, Organization>
    {
        #region Constructors

        /// <summary>
        /// Creates organization list
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>organization_list</c> and other optional elements</param>
        public OrganizationDictionary(Dictionary<string, object> obj)
        {
            foreach (var el in eduJSON.Parser.GetValue<List<object>>(obj, "organization_list"))
            {
                if (!(el is Dictionary<string, object> obj2))
                    continue;
                var entry = new Organization(obj2);
                Add(entry.Id, entry);
            }
        }

        #endregion
    }
}

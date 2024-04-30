/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
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
        #region Utf8Json

        public class Json
        {
            public List<Organization.Json> organization_list { get; set; }
        }

        /// <summary>
        /// Creates organization list
        /// </summary>
        /// <param name="json">JSON object</param>
        public OrganizationDictionary(Json json)
        {
            if (json.organization_list != null) {
                foreach (var el in json.organization_list)
                {
                    var entry = new Organization(el);
                    Add(entry.Id, entry);
                }
            }
        }

        #endregion
    }
}

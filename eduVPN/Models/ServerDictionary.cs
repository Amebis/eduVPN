/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// List of eduVPN servers
    /// </summary>
    public class ServerDictionary : Dictionary<string, Server>
    {
        #region Utf8Json

        public class Json
        {
            public List<Server.Json> server_list { get; set; }
        }

        /// <summary>
        /// Creates server list
        /// </summary>
        /// <param name="json">JSON object</param>
        public ServerDictionary(Json json)
        {
            if (json.server_list != null)
            {
                foreach (var el in json.server_list)
                {
                    if (el.server_type == null)
                        throw new ArgumentException();
                    Server entry;
                    switch (el.server_type.ToLower())
                    {
                        case "institute_access": entry = new InstituteAccessServer(el); break;
                        case "secure_internet": entry = new SecureInternetServer(el); break;
                        default: throw new ArgumentOutOfRangeException("server_type");
                    }
                    Add(entry.Id, entry);
                }
            }
        }

        #endregion
    }
}

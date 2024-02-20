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
        #region Constructors

        /// <summary>
        /// Creates server list
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>server_list</c> and other optional elements</param>
        public ServerDictionary(Dictionary<string, object> obj)
        {
            foreach (var el in eduJSON.Parser.GetValue<List<object>>(obj, "server_list"))
            {
                if (!(el is Dictionary<string, object> obj2))
                    throw new eduJSON.InvalidParameterTypeException("server_list", typeof(Dictionary<string, object>), el.GetType());

                if (!eduJSON.Parser.GetValue(obj2, "server_type", out string server_type) || server_type == null)
                    throw new eduJSON.MissingParameterException("server_type");
                Server entry;
                switch (server_type.ToLower())
                {
                    case "institute_access": entry = new InstituteAccessServer(obj2); break;
                    case "secure_internet": entry = new SecureInternetServer(obj2); break;
                    default: throw new ArgumentOutOfRangeException("server_type");
                }
                Add(entry.Id, entry);
            }
        }

        #endregion
    }
}

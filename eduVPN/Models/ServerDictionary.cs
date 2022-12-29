/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// List of eduVPN servers
    /// </summary>
    public class ServerDictionary : Dictionary<Uri, Server>, JSON.ILoadableItem
    {
        #region ILoadableItem Support

        /// <summary>
        /// Loads server list from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>server_list</c> and other optional elements</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public virtual void Load(object obj)
        {
            if (!(obj is Dictionary<string, object> obj2))
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());

            // Parse all servers listed.
            Clear();
            foreach (var el in eduJSON.Parser.GetValue<List<object>>(obj2, "server_list"))
            {
                if (!(el is Dictionary<string, object> obj3))
                    throw new eduJSON.InvalidParameterTypeException("server_list", typeof(Dictionary<string, object>), el.GetType());

                Server entry;
                if (!eduJSON.Parser.GetValue(obj3, "server_type", out string server_type))
                    throw new eduJSON.MissingParameterException("server_type");
                switch (server_type.ToLower())
                {
                    case "institute_access": entry = new InstituteAccessServer(); break;
                    case "secure_internet": entry = new SecureInternetServer(); break;
                    default: throw new ArgumentOutOfRangeException("server_type");
                }
                entry.Load(el);
                Add(entry.Base, entry);
            }
        }

        #endregion
    }
}

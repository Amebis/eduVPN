/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;
using System.Threading;

namespace eduVPN.JSON
{
    public static class Extensions
    {
        /// <summary>
        /// Loads class from a JSON string
        /// </summary>
        /// <param name="i">Loadable item</param>
        /// <param name="json">JSON string</param>
        /// <param name="ct">The token to monitor for cancellation requests.</param>
        public static void LoadJSON(this ILoadableItem i, string json, CancellationToken ct = default(CancellationToken))
        {
            i.Load(eduJSON.Parser.Parse(json, ct));
        }

        /// <summary>
        /// Loads class from a JSON string provided by API
        /// </summary>
        /// <param name="i">Loadable item</param>
        /// <param name="json">JSON string representing a dictionary of key/values with <paramref name="name"/> element.</param>
        /// <param name="name">The name of the value holder element</param>
        /// <param name="ct">The token to monitor for cancellation requests.</param>
        public static void LoadJSONAPIResponse(this ILoadableItem i, string json, string name, CancellationToken ct = default(CancellationToken))
        {
            // Parse JSON string and get inner key/value dictionary.
            var obj = eduJSON.Parser.GetValue<Dictionary<string, object>>(
                (Dictionary<string, object>)eduJSON.Parser.Parse(json, ct),
                name);

            // Verify response status.
            if (eduJSON.Parser.GetValue(obj, "ok", out bool is_ok) && !is_ok)
                throw new APIErrorException();

            // Load data.
            i.Load(obj["data"]);
        }
    }
}

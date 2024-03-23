/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Threading;

namespace eduVPN.JSON
{
    /// <summary>
    /// <see cref="JSON"/> namespace extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Loads class from a JSON string
        /// </summary>
        /// <param name="i">Loadable item</param>
        /// <param name="json">JSON string</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        public static void LoadJSON(this ILoadableItem i, string json, CancellationToken ct = default)
        {
            i.Load(eduJSON.Parser.Parse(json, ct));
        }
    }
}

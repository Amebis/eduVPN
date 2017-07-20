/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

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
    }
}

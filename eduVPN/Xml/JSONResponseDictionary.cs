/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable dictionary of sequenced JSON responses
    /// </summary>
    public class JSONResponseDictionary : Dictionary<Response>
    {
        #region Fields

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object _lock = new object();

        #endregion

        #region Methods

        /// <summary>
        /// Gets sequenced JSON from the given URI.
        /// </summary>
        /// <param name="res">URI and public key for signature verification</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>JSON content</returns>
        public Dictionary<string, object> GetSeq(ResourceRef res, CancellationToken ct = default)
        {
            // Retrieve response from cache (if available).
            var key = res.Uri.AbsoluteUri;
            Response response_cache = null;
            lock (_lock)
                if (!TryGetValue(key, out response_cache))
                    response_cache = null;

            // Get instance source.
            var response_web = Xml.Response.Get(
                res: res,
                ct: ct,
                previous: response_cache);

            // Parse instance source JSON.
            var obj_web = (Dictionary<string, object>)eduJSON.Parser.Parse(response_web.Value, ct);

            if (response_web.IsFresh)
            {
                // Save response to cache.
                lock (_lock)
                    this[key] = response_web;
            }

            return obj_web;
        }

        #endregion
    }
}

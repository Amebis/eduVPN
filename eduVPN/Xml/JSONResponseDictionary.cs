/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Threading;

namespace eduVPN.Xml
{
    /// <summary>
    /// Serializable dictionary of sequenced JSON responses
    /// </summary>
    public class JSONResponseDictionary : Dictionary<Response>
    {
        #region Fields

        object _lock = new object();

        #endregion

        #region Methods

        /// <summary>
        /// Gets sequenced JSON from the given URI.
        /// </summary>
        /// <param name="uri">URI</param>
        /// <param name="pub_key">Public key for signature verification; or <c>null</c> if signature verification is not required</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>JSON content</returns>
        public Dictionary<string, object> GetSeq(Uri uri, byte[] pub_key = null, CancellationToken ct = default(CancellationToken))
        {
            // Retrieve response from cache (if available).
            var key = uri.AbsoluteUri;
            Response response_cache = null;
            lock (_lock)
                if (!TryGetValue(key, out response_cache))
                    response_cache = null;

            // Get instance source.
            var response_web = Xml.Response.Get(
                uri: uri,
                pub_key: pub_key,
                ct: ct,
                previous: response_cache);

            // Parse instance source JSON.
            var obj_web = (Dictionary<string, object>)eduJSON.Parser.Parse(response_web.Value, ct);

            if (response_web.IsFresh)
            {
                if (response_cache != null)
                {
                    try
                    {
                        // Verify sequence.
                        var obj_cache = (Dictionary<string, object>)eduJSON.Parser.Parse(response_cache.Value, ct);

                        bool rollback = false;
                        try { rollback = (uint)eduJSON.Parser.GetValue<int>(obj_cache, "seq") > (uint)eduJSON.Parser.GetValue<int>(obj_web, "seq"); }
                        catch { rollback = true; }
                        if (rollback)
                        {
                            // Sequence rollback detected. Revert to cached version.
                            obj_web = obj_cache;
                            response_web = response_cache;
                        }
                    }
                    catch { }
                }

                // Save response to cache.
                lock (_lock)
                    this[key] = response_web;
            }

            return obj_web;
        }

        #endregion
    }
}

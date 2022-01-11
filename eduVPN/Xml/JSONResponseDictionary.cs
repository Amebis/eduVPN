/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
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
        private readonly object Lock = new object();

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
            Response responseCache = null;
            lock (Lock)
                if (!TryGetValue(key, out responseCache))
                    responseCache = null;

            // Get JSON.
            var webResponse = Xml.Response.Get(
                res: res,
                ct: ct,
                previous: responseCache);

            // Parse JSON.
            var objWeb = (Dictionary<string, object>)eduJSON.Parser.Parse(webResponse.Value, ct);

            if (webResponse.IsFresh)
            {
                if (responseCache != null)
                {
                    // Verify version.
                    var objCache = (Dictionary<string, object>)eduJSON.Parser.Parse(responseCache.Value, ct);
                    if (eduJSON.Parser.GetValue(objCache, "v", out int vCache))
                    {
                        if (!eduJSON.Parser.GetValue(objWeb, "v", out int vWeb) ||
                            vWeb <= vCache)
                        {
                            // Version rollback detected. Revert to cached version.
                            objWeb = objCache;
                            webResponse = responseCache;
                        }
                    }
                }

                // Save response to cache.
                lock (Lock)
                    this[key] = webResponse;
            }

            return objWeb;
        }

        /// <summary>
        /// Removes all cache entries older than 6 months
        /// </summary>
        public void PurgeOldCacheEntries()
        {
            var threshold = DateTime.Now.AddMonths(-6);
            lock (Lock)
                this.RemoveAll(el => el.Timestamp <= threshold);
        }

        #endregion
    }
}

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
        /// <returns>JSON content</returns>
        public Response GetSeqFromCache(ResourceRef res)
        {
            var key = res.Uri.AbsoluteUri;
            lock (Lock)
                return TryGetValue(key, out var value) ? value : null;
        }


        /// <summary>
        /// Gets sequenced JSON from the given URI.
        /// </summary>
        /// <param name="res">URI and public key for signature verification</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>JSON content</returns>
        public Dictionary<string, object> GetSeq(ResourceRef res, CancellationToken ct = default)
        {
            // Retrieve response from cache.
            var cachedResponse = GetSeqFromCache(res);

            // Get JSON.
            var webResponse = Xml.Response.Get(
                res: res,
                ct: ct,
                previous: cachedResponse);

            // Parse JSON.
            var objWeb = (Dictionary<string, object>)eduJSON.Parser.Parse(webResponse.Value, ct);

            if (webResponse.IsFresh)
            {
                if (cachedResponse != null)
                {
                    // Verify version.
                    var objCache = (Dictionary<string, object>)eduJSON.Parser.Parse(cachedResponse.Value, ct);
                    if (eduJSON.Parser.GetValue(objCache, "v", out long vCache))
                    {
                        if (!eduJSON.Parser.GetValue(objWeb, "v", out long vWeb) ||
                            vWeb <= vCache)
                        {
                            // Version rollback detected. Revert to cached version.
                            objWeb = objCache;
                            webResponse = cachedResponse;
                        }
                    }
                }

                // Save response to cache.
                lock (Lock)
                    this[res.Uri.AbsoluteUri] = webResponse;
            }

            return objWeb;
        }

        /// <summary>
        /// Removes all cache entries older than 6 months
        /// </summary>
        public void PurgeOldCacheEntries()
        {
            var now = DateTimeOffset.Now;
            var threshold = now.AddMonths(-6);
            lock (Lock)
                this.RemoveAll(el => el.Expires <= now || el.LastModified <= threshold);
        }

        #endregion
    }
}

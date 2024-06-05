/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace eduVPN.Models
{
    /// <summary>
    /// Session expiry times
    /// </summary>
    public class Expiration
    {
        #region Properties

        /// <summary>
        /// Time VPN session was started
        /// </summary>
        public DateTimeOffset StartedAt { get; }

        /// <summary>
        /// Time VPN session will end
        /// </summary>
        public DateTimeOffset EndAt { get; }

        /// <summary>
        /// Time at which to start showing the renew button in the UI
        /// </summary>
        public DateTimeOffset ButtonAt { get; }

        /// <summary>
        /// Time at which to start showing more detailed countdown timer
        /// </summary>
        public DateTimeOffset CountdownAt { get; }

        /// <summary>
        /// Times at which to show a notification that the VPN is about to expire
        /// </summary>
        public List<DateTimeOffset> NotificationAt { get; }

        #endregion

        #region Utf8Json

        public class Json
        {
            public long start_time { get; set; }
            public long end_time { get; set; }
            public long button_time { get; set; }
            public long countdown_time { get; set; }
            public List<long> notification_times { get; set; }
        }

        /// <summary>
        /// Creates session expiry times
        /// </summary>
        /// <param name="json">JSON object</param>
        public Expiration(Json json)
        {
            StartedAt = FromUnixTimeSeconds(json.start_time);
            EndAt = FromUnixTimeSeconds(json.end_time);
            ButtonAt = FromUnixTimeSeconds(json.button_time);
            CountdownAt = FromUnixTimeSeconds(json.countdown_time);
            NotificationAt = json.notification_times != null ? json.notification_times
                .Where(value => json.start_time <= value && value <= json.end_time - 5)
                .Select(value => FromUnixTimeSeconds(value))
                .ToList() :
                new List<DateTimeOffset>();
        }

        /// <summary>
        /// Converts Unix timestamp to DateTimeOffset.
        /// </summary>
        /// <note>Values outside of -62135596800 and 253402300799 Unix timestamps (inclusive) may not be represented by DateTimeOffset and are converted to DateTimeOffset.MinValue and DateTimeOffset.MaxValue respectively.</note>
        /// <param name="unixTime">Unix timestamp</param>
        /// <returns>A DateTimeOffset representation of Unix timestamp</returns>
        DateTimeOffset FromUnixTimeSeconds(long unixTime)
        {
            return
                unixTime < -62135596800 ? DateTimeOffset.MinValue :
                unixTime > 253402300799 ? DateTimeOffset.MaxValue :
                    DateTimeOffset.FromUnixTimeSeconds(unixTime);
        }

        #endregion
    }
}

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
    /// VPN expiry times
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

        #region Constructors

        /// <summary>
        /// Creates VPN configuration
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>start_time</c>, <c>end_time</c>, <c>button_time</c>, <c>countdown_time</c> and <c>notification_times</c> elements.</param>
        public Expiration(IReadOnlyDictionary<string, object> obj)
        {
            long startedAtUnix, endAtUnix;
            StartedAt = DateTimeOffset.FromUnixTimeSeconds(startedAtUnix = eduJSON.Parser.GetValue<long>(obj, "start_time"));
            EndAt = DateTimeOffset.FromUnixTimeSeconds(endAtUnix = eduJSON.Parser.GetValue<long>(obj, "end_time"));
            ButtonAt = DateTimeOffset.FromUnixTimeSeconds(eduJSON.Parser.GetValue<long>(obj, "button_time"));
            CountdownAt = DateTimeOffset.FromUnixTimeSeconds(eduJSON.Parser.GetValue<long>(obj, "countdown_time"));
            NotificationAt = eduJSON.Parser.GetValue<List<object>>(obj, "notification_times", out var notification_times) ? notification_times
                .Where(value => value is long l && startedAtUnix <= l && l <= endAtUnix - 5)
                .Select(value => DateTimeOffset.FromUnixTimeSeconds((long)value))
                .ToList() :
                new List<DateTimeOffset>();
        }

        #endregion
    }
}

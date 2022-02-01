/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Localization;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace eduVPN.Converters
{
    /// <summary>
    /// Returns <see cref="TimeSpan"/> in human readable localized form
    /// </summary>
    public class TimeSpanToHumanReadableConverter : IValueConverter
    {
        #region Fields

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly TimeSpan OneDay = new TimeSpan(1, 0, 0, 0);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly TimeSpan OneHour = new TimeSpan(0, 1, 0, 0);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly TimeSpan OneMinute = new TimeSpan(0, 0, 1, 0);

        #endregion

        #region Methods

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>A converted value. If the method returns <c>null</c>, the valid null value is used.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TimeSpan timespan))
                return null;
            if (timespan == TimeSpan.MaxValue)
                return Views.Resources.Strings.TimeSpanNotAvailable;
            var formatProvider = new CardinalPluralFormatProvider();
            if (timespan > OneDay)
                return string.Format(formatProvider, Views.Resources.Strings.TimeSpanDaysAndHours, timespan.Days, timespan.Hours);
            if (timespan > OneHour)
                return string.Format(formatProvider, Views.Resources.Strings.TimeSpanHoursAndMinutes, timespan.Hours, timespan.Minutes);
            if (timespan > OneMinute)
                return string.Format(formatProvider, Views.Resources.Strings.TimeSpanMinutesAndSeconds, timespan.Minutes, timespan.Seconds);
            return string.Format(formatProvider, Views.Resources.Strings.TimeSpanSeconds, timespan.Seconds);
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target</param>
        /// <param name="targetType">The type to convert to</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        /// <exception cref="NotImplementedException">Always</exception>
        /// <remarks>Not implemented.</remarks>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

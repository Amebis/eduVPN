/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Globalization;
using System.Windows.Data;

namespace eduVPN.Client.Converters
{
    /// <summary>
    /// Returns <c>TimeSpan</c> up to seconds accurate
    /// </summary>
    public class TimeSpanInSecondsConverter : IValueConverter
    {
        private static TimeSpan _one_day = new TimeSpan(1, 0, 0, 0);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is TimeSpan timespan ?
                timespan.ToString(
                    timespan < _one_day ?
                        Resources.Strings.TimeSpanInSeconds :
                        Resources.Strings.TimeSpanInSecondsWithDays,
                    culture) :
                null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

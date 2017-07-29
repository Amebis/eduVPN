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
    /// Returns readable exception message.
    /// </summary>
    public class ExceptionMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AggregateException ex_agg)
                return ex_agg.Message + "\r\n" + new ExceptionMessageConverter().Convert(ex_agg.InnerException, targetType, parameter, culture);
            else if (value is Exception ex)
                return ex.Message;
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

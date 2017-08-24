/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace eduVPN.Client.Converters
{
    /// <summary>
    /// Returns window icon according to status state.
    /// </summary>
    public class VPNSessionStatusTypeWindowIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is VPNSessionStatusType status_type && status_type == VPNSessionStatusType.Connected)
                return new BitmapImage(new Uri("pack://application:,,,/Resources/eduVPNConnected.ico"));
            else
                return new BitmapImage(new Uri("pack://application:,,,/Resources/eduVPN.ico"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

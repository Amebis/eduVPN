/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.VPN;
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
            if (value is VPNSessionStatusType status_type)
            {
                try
                {
                    return new BitmapImage(
                        new Uri(
                            String.Format(
                                "pack://application:,,,/Resources/VPNSessionStatusTypeIcon{0}.ico",
                                Enum.GetName(typeof(VPNSessionStatusType), status_type))));
                }
                catch { }
            }

            return new BitmapImage(new Uri("pack://application:,,,/Resources/VPNSessionStatusTypeIconInitializing.ico"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

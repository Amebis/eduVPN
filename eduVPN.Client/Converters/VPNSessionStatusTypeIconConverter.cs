/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.VPN;
using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace eduVPN.Client.Converters
{
    /// <summary>
    /// Returns status icon according to status state.
    /// </summary>
    public class VPNSessionStatusTypeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is VPNSessionStatusType status_type)
            {
                var image_uri = new Uri(String.Format("pack://application:,,,/Resources/VPNSessionStatusTypeIcon{0}.png", Enum.GetName(typeof(VPNSessionStatusType), status_type)));
                try {
                    // If resource with given image URI exist, return the URI.
                    Application.GetResourceStream(image_uri);
                    return image_uri;
                }
                catch (IOException) { }
            }

            // Fallback to blank image.
            return new Uri("pack://application:,,,/Resources/Blank.png");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOpenVPN;
using eduVPN.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
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
                var canvas = Application.Current.TryFindResource(String.Format("eduVPNSessionStatusTypeIcon{0}", Enum.GetName(typeof(VPNSessionStatusType), status_type)));
                return canvas != null ? canvas as Canvas : Application.Current.TryFindResource("eduVPNVPNSessionStatusTypeInitializingIcon") as Canvas;
            }
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

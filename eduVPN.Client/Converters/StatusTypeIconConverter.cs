/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

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
    public class StatusTypeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.StatusType)
            {
                switch ((Models.StatusType)value)
                {
                    case Models.StatusType.Initializing: return Application.Current.TryFindResource("eduVPNStatusTypeInitializingIcon") as Canvas;
                    case Models.StatusType.Connecting: return Application.Current.TryFindResource("eduVPNStatusTypeConnectingIcon") as Canvas;
                    case Models.StatusType.Connected: return Application.Current.TryFindResource("eduVPNStatusTypeConnectedIcon") as Canvas;
                    default: return null;
                }
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

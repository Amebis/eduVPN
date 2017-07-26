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
    /// Returns text description according to status state.
    /// </summary>
    public class StatusTypeDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.StatusType)
            {
                switch ((Models.StatusType)value)
                {
                    case Models.StatusType.Initializing: return Resources.Strings.StatusTypeInitializing;
                    case Models.StatusType.Connecting: return Resources.Strings.StatusTypeConnecting;
                    case Models.StatusType.Connected: return Resources.Strings.StatusTypeConnected;
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

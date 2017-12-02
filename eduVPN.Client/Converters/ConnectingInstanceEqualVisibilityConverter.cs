/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace eduVPN.Client.Converters
{
    /// <summary>
    /// Returns <c>Visibility.Visible</c> if <c>value[0]</c> represent the same instance as <c>value[1]</c>; or <c>Visibility.Collapsed</c> otherwise.
    /// </summary>
    public class ConnectingInstanceEqualVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return
                values[0] is Instance instance1 &&
                values[1] is Instance instance2 &&
                instance1.Equals(instance2) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

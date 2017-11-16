/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace eduVPN.Client.Converters
{
    /// <summary>
    /// Returns <c>Visibility.Visible</c> if <c>value[0]</c> and <c>value[1]</c> represent the same instance and profile as <c>value[2]</c> and <c>value[3]</c>; or <c>Visibility.Collapsed</c> otherwise.
    /// </summary>
    public class ConnectingInstanceAndProfileEqualVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return
                values[0] is Models.Instance instance1 &&
                values[1] is Models.Profile profile1 &&
                values[2] is Models.Instance instance2 &&
                values[3] is Models.Profile profile2 &&
                instance1.Equals(instance2) &&
                profile1.Equals(profile2) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

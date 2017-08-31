/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace eduVPN.Client.Converters
{
    /// <summary>
    /// Returns <c>Visibility.Collapsed</c> if configuration histories contains any records; or <c>Visibility.Visible</c> otherwise.
    /// </summary>
    class HistorySourceInverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return
                value is ObservableCollection<Models.VPNConfiguration>[] configuration_histories &&
                parameter is Models.InstanceSourceType instance_source_type &&
                configuration_histories[(int)instance_source_type].Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

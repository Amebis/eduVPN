/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace eduVPN.Client.Converters
{
    /// <summary>
    /// Returns <c>Visibility.Collapsed</c> if instance source contains any connecting instances; or <c>Visibility.Visible</c> otherwise.
    /// </summary>
    class InstanceSourceInverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return
                value is ConnectWizard parent &&
                parameter is InstanceSourceType instance_source_type &&
                parent.InstanceSources[(int)instance_source_type].ConnectingInstance != null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

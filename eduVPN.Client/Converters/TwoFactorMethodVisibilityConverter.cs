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
    /// Returns <c>Visibility.Visible</c> for a non-null subset of 2-Factor Authentication methods; or <c>Visibility.Collapsed</c> otherwise.
    /// </summary>
    public class TwoFactorMethodVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return
                value is TwoFactorAuthenticationMethods methods &&
                methods != TwoFactorAuthenticationMethods.None &&
                methods != TwoFactorAuthenticationMethods.Any ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace eduVPN.Client.Converters
{
    /// <summary>
    /// Converts and returns TOTP secret as human readable string
    /// </summary>
    /// <remarks>Only integer numbers supported</remarks>
    public class TOTPSecretConverter : BindableBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string secret)
            {
                // Divide line in 4-tuples separated by spaces.
                int group_counter = 0;
                return string.Join(
                    " ",
                    secret
                        .GroupBy(_ => group_counter++ / 4)
                        .Select(g => new String(g.ToArray())));
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

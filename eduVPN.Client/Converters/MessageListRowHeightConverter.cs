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
    /// Returns <c>1*</c> if message list contains at least one message; or <c>Auto</c> otherwise.
    /// </summary>
    public class MessageListRowHeightConverter : IValueConverter
    {
        #region Fields

        /// <summary>
        /// 1* grid length
        /// </summary>
        private static readonly GridLength _one_star_grid_length = new GridLength(1.0, GridUnitType.Star);

        #endregion

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int count && count > 0 ? _one_star_grid_length : GridLength.Auto;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

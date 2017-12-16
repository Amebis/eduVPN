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
    /// Returns <c>1*</c> if instance source contains any connecting instances; or <c>Auto</c> otherwise.
    /// </summary>
    class InstanceSourceRowHeightConverter : IValueConverter
    {
        #region Fields

        /// <summary>
        /// 1* grid length
        /// </summary>
        private static readonly GridLength _one_star_grid_length = new GridLength(1.0, GridUnitType.Star);

        #endregion

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value as Instance != null ? _one_star_grid_length : GridLength.Auto;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

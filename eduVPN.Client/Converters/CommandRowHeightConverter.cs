/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace eduVPN.Client.Converters
{
    /// <summary>
    /// Returns <c>GridLength.Auto</c> if command can execute; or <c>0</c> otherwise.
    /// </summary>
    public class CommandRowHeightConverter : IValueConverter
    {
        #region Fields

        /// <summary>
        /// 1* grid length
        /// </summary>
        private static readonly GridLength _zero_grid_length = new GridLength(0);

        #endregion

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is ICommand command && command.CanExecute(null) ? GridLength.Auto : _zero_grid_length;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

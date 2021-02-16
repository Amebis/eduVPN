/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;

namespace eduVPN.Converters
{
    /// <summary>
    /// Returns <see cref="FlowDirection.RightToLeft"/> if input value is "RTL" string; or <see cref="FlowDirection.LeftToRight"/> otherwise.
    /// </summary>
    public class FlowDirectionConverter : IValueConverter
    {
        #region Methods

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>A converted value. If the method returns <c>null</c>, the valid null value is used.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string str && string.Equals(str, "RTL", StringComparison.OrdinalIgnoreCase) ?
                System.Windows.FlowDirection.RightToLeft :
                System.Windows.FlowDirection.LeftToRight;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target</param>
        /// <param name="targetType">The type to convert to</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        /// <exception cref="NotImplementedException">Always</exception>
        /// <remarks>Not implemented.</remarks>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is System.Windows.FlowDirection fd && fd == System.Windows.FlowDirection.RightToLeft ? "RTL" : "LTR";
        }

        #endregion
    }
}

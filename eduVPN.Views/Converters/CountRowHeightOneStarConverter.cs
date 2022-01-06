/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace eduVPN.Converters
{
    /// <summary>
    /// Returns <c>1*</c> if the value is non-zero; or <c>0px</c> otherwise.
    /// </summary>
    public class CountRowHeightOneStarConverter : IValueConverter
    {
        #region Fields

        /// <summary>
        /// 1* grid length
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly GridLength OneStarGridLength = new GridLength(1.0, GridUnitType.Star);

        /// <summary>
        /// 0 grid length
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly GridLength ZeroGridLength = new GridLength(0, GridUnitType.Pixel);

        #endregion

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
            return value is int i && i > 0 ? OneStarGridLength : ZeroGridLength;
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
            throw new NotImplementedException();
        }

        #endregion
    }
}

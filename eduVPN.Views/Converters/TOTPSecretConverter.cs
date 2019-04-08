/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace eduVPN.Converters
{
    /// <summary>
    /// Converts and returns TOTP secret as human readable string
    /// </summary>
    /// <remarks>Only integer numbers supported</remarks>
    public class TOTPSecretConverter : BindableBase, IValueConverter
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
            if (value is string secret)
            {
                // Divide line in 4-tuples separated by spaces.
                int group_counter = 0;
                return String.Join(
                    " ",
                    secret
                        .GroupBy(_ => group_counter++ / 4)
                        .Select(g => new String(g.ToArray())));
            }

            return null;
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

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace eduVPN.Converters
{
    /// <summary>
    /// Returns string with access key underscore removed (if any).
    /// </summary>
    public class RemoveAccessKeyConverter : IValueConverter
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
            if (value is string str)
            {
                var sb = new StringBuilder();
                var accessKey = '\0';
                for (int i = 0, n = str.Length; i < n; i++)
                {
                    var nextIndex = i + 1;
                    if (nextIndex < n)
                    {
                        if (str[i] == '_')
                        {
                            // Underscore
                            if (str[nextIndex] == '_')
                            {
                                // Double underscore: Convert to single underscore.
                                sb.Append('_');
                                i = nextIndex;
                                continue;
                            }
                            else if (accessKey == '\0')
                            {
                                // Save the access key.
                                accessKey = str[nextIndex];
                                continue;
                            }
                        }
                    }

                    // Should read-out-loud give better results without line-breaks,
                    // replace them with double spaces.
                    //if (str[i] == '\n')
                    //{
                    //    // Replace LF with double space.
                    //    sb.Append("  ");
                    //    continue;
                    //}
                    //else if (str[i] == '\r')
                    //{
                    //    // Remove CR.
                    //    continue;
                    //}

                    // Other chars: Append.
                    sb.Append(str[i]);
                }

                return sb.ToString();
            }
            else
                return value;
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

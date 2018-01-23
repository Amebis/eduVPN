/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace eduVPN.Client.Converters
{
    /// <summary>
    /// Returns string with access key underscore removed (if any).
    /// </summary>
    public class RemoveAccessKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                var sb = new StringBuilder();
                char access_key = '\0';
                for (int i = 0, n = str.Length; i < n; i++)
                {
                    var i_next = i + 1;
                    if (i_next < n)
                    {
                        if (str[i] == '_')
                        {
                            // Underscore
                            if (str[i_next] == '_')
                            {
                                // Double underscore: Convert to single underscore.
                                sb.Append('_');
                                i = i_next;
                                continue;
                            }
                            else if (access_key == '\0')
                            {
                                // Save the access key.
                                access_key = str[i_next];
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

﻿/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.VPN;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace eduVPN.Converters
{
    /// <summary>
    /// Returns status icon according to status state.
    /// </summary>
    public class VPNSessionStatusTypeIconConverter : IValueConverter
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
            if (value is VPNSessionStatusType statusType)
            {
                var resourceName = string.Format("VPNSessionStatusType{0}Icon", Enum.GetName(typeof(VPNSessionStatusType), statusType));
                if (Application.Current.Resources.Contains(resourceName))
                    return Application.Current.Resources[resourceName];
            }
            return Application.Current.Resources.Contains("VPNSessionStatusTypeIcon") ? Application.Current.Resources["VPNSessionStatusTypeIcon"] : null;
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

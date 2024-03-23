﻿/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace eduVPN.Converters
{
    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> if Renewal button should be displayed; or <see cref="Visibility.Collapsed"/> otherwise.
    /// </summary>
    public class RenewVisibilityConverter : IMultiValueConverter
    {
        #region Methods

        /// <summary>
        /// Converts source values to a value for the binding target. The data binding engine calls this method when it propagates the values from source bindings to the binding target.
        /// </summary>
        /// <param name="values">The array of values that the source bindings in the <see cref="MultiBinding"/> produces. The value <see cref="DependencyProperty.UnsetValue"/> indicates that the source binding has no value to provide for conversion.</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>
        /// A converted value.
        /// If the method returns <c>null</c>, the valid null value is used.
        /// A return value of <see cref="DependencyProperty.UnsetValue"/> indicates that the converter did not produce a value, and that the binding will use the <see cref="BindingBase.FallbackValue"/> if it is available, or else will use the default value.
        /// A return value of <see cref="Binding.DoNothing"/> indicates that the binding does not transfer the value or use the <see cref="BindingBase.FallbackValue"/> or the default value.
        /// </returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            return
                values[1] is DateTimeOffset from && from != DateTimeOffset.MinValue &&
                values[2] is DateTimeOffset to && to != DateTimeOffset.MaxValue &&
#if DEBUG
                (now - from).TotalMinutes > 1 &&
#else
                (now - from).TotalMinutes > 30 &&
#endif
                (to - now).TotalHours < 24 ?
                Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Converts a binding target value to the source binding values.
        /// </summary>
        /// <param name="value">The value that the binding target produces</param>
        /// <param name="targetTypes">The array of types to convert to. The array length indicates the number and types of values that are suggested for the method to return.</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>An array of values that have been converted from the target value back to the source values</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

#endregion
    }
}

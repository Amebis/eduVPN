/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Globalization;
using System.Windows.Data;

namespace eduVPN.Converters
{
    /// <summary>
    /// Converts and returns number as human readable string using metric prefixes
    /// </summary>
    /// <remarks>Only integer numbers supported</remarks>
    public class NumberToHumanReadableConverter : BindableBase, IValueConverter
    {
        #region Fields

        private static string[] _prefixes = new string[] { "", "k", "M", "G", "T", "P", "E" };

        #endregion

        #region Properties

        /// <summary>
        /// Unit of measure (Default empty)
        /// </summary>
        public string Unit
        {
            get { return _unit; }
            set { SetProperty(ref _unit, value); }
        }
        private string _unit = "";

        /// <summary>
        /// Metric base (Default 1000)
        /// </summary>
        public int Base
        {
            get { return _base; }
            set { SetProperty(ref _base, value); }
        }
        private int _base = 1000;

        /// <summary>
        /// Return empty string when number is 0?
        /// </summary>
        public bool EmptyIfZero
        {
            get { return _empty_if_zero; }
            set { SetProperty(ref _empty_if_zero, value); }
        }
        private bool _empty_if_zero = false;

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
            if (value == null)
                return null;

            double number = System.Convert.ToDouble(value);
            int b = parameter != null ? System.Convert.ToInt32(parameter) : _base;

            if (number <= 0.5 && _empty_if_zero)
                return "";

            int n = number > 0.5 ? Math.Min((int)Math.Truncate(Math.Log(Math.Abs(number)) / Math.Log(b)), _prefixes.Length) : 0;
            double x = number / Math.Pow(b, n);
            return String.Format(
                Views.Resources.Strings.NumberToHumanReadable,
                Math.Abs(x) < 10 ?
                    (Math.Truncate(x * 10)/10).ToString("N1") :
                     Math.Truncate(x     )    .ToString(    ),
                _prefixes[n],
                _unit);
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

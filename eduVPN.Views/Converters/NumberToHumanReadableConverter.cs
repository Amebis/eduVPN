/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Diagnostics;
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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly string[] Prefixes = new string[] { "", "k", "M", "G", "T", "P", "E" };

        #endregion

        #region Properties

        /// <summary>
        /// Unit of measure (Default empty)
        /// </summary>
        public string Unit
        {
            get { return _Unit; }
            set { SetProperty(ref _Unit, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _Unit = "";

        /// <summary>
        /// Metric base (Default 1000)
        /// </summary>
        public int Base
        {
            get { return _Base; }
            set { SetProperty(ref _Base, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _Base = 1000;

        /// <summary>
        /// Return empty string when number is 0?
        /// </summary>
        public bool EmptyIfZero
        {
            get { return _EmptyIfZero; }
            set { SetProperty(ref _EmptyIfZero, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _EmptyIfZero = false;

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
            int b = parameter != null ? System.Convert.ToInt32(parameter) : Base;

            if (number <= 0.5 && EmptyIfZero)
                return "";

            int n = number > 0.5 ? Math.Min((int)Math.Truncate(Math.Log(Math.Abs(number)) / Math.Log(b)), Prefixes.Length) : 0;
            double x = number / Math.Pow(b, n);
            return string.Format(
                Views.Resources.Strings.NumberToHumanReadable,
                n > 0 && Math.Abs(x) < 10 ?
                    (Math.Truncate(x * 10) / 10).ToString("N1") :
                     Math.Truncate(x).ToString(),
                Prefixes[n],
                Unit);
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

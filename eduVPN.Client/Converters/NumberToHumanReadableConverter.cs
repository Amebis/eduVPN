/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Globalization;
using System.Windows.Data;

namespace eduVPN.Client.Converters
{
    /// <summary>
    /// Converts and returns number as human readable string using metric prefixes
    /// </summary>
    /// <remarks>Only integer numbers supported</remarks>
    class NumberToHumanReadableConverter : BindableBase, IValueConverter
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
            set { if (value != _unit) { _unit = value; RaisePropertyChanged(); } }
        }
        private string _unit = "";

        /// <summary>
        /// Metric base (Default 1000)
        /// </summary>
        public int Base
        {
            get { return _base; }
            set { if (value != _base) { _base = value; RaisePropertyChanged(); } }
        }
        private int _base = 1000;

        /// <summary>
        /// Return empty string when number is 0?
        /// </summary>
        public bool EmptyIfZero
        {
            get { return _empty_if_zero; }
            set { if (value != _empty_if_zero) { _empty_if_zero = value; RaisePropertyChanged(); } }
        }
        private bool _empty_if_zero = false;

        #endregion

        #region Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            double number = System.Convert.ToDouble(value);
            int _base = parameter != null ? System.Convert.ToInt32(parameter) : 1000;

            if (number <= 0.5 && _empty_if_zero)
                return "";

            int n = number > 0.5 ? Math.Min((int)Math.Truncate(Math.Log(Math.Abs(number)) / Math.Log(_base) + 0.2), _prefixes.Length) : 0;
            return String.Format(
                Resources.Strings.NumberToHumanReadable,
                n > 0 ?
                    Math.Truncate(number / Math.Pow(_base, n)) :
                    number,
                _prefixes[n],
                _unit);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;

namespace eduVPN
{
    /// <summary>
    /// Range descriptor
    /// </summary>
    public class Range<T> : BindableBase
    {
        #region Properties

        /// <summary>
        /// Range minimum value
        /// </summary>
        public T Minimum
        {
            get { return _minimum; }
            set { _minimum = value; RaisePropertyChanged(); }
        }
        private T _minimum;

        /// <summary>
        /// Range maximum value
        /// </summary>
        public T Maximum
        {
            get { return _maximum; }
            set { _maximum = value; RaisePropertyChanged(); }
        }
        private T _maximum;

        /// <summary>
        /// Range current value
        /// </summary>
        public T Value
        {
            get { return _value; }
            set { _value = value; RaisePropertyChanged(); }
        }
        private T _value;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a range descriptor
        /// </summary>
        public Range()
        {
        }

        /// <summary>
        /// Constructs a range descriptor
        /// </summary>
        public Range(T min, T max)
        {
            Minimum = min;
            Maximum = max;
        }

        /// <summary>
        /// Constructs a range descriptor
        /// </summary>
        public Range(T min, T max, T val) :
            this(min, max)
        {
            Value = val;
        }

        #endregion
    }
}

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System.Diagnostics;

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
            set { SetProperty(ref _minimum, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private T _minimum;

        /// <summary>
        /// Range maximum value
        /// </summary>
        public T Maximum
        {
            get { return _maximum; }
            set { SetProperty(ref _maximum, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private T _maximum;

        /// <summary>
        /// Range current value
        /// </summary>
        public T Value
        {
            get { return _value; }
            set { SetProperty(ref _value, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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
            _minimum = min;
            _maximum = max;
        }

        /// <summary>
        /// Constructs a range descriptor
        /// </summary>
        public Range(T min, T max, T val) :
            this(min, max)
        {
            _value = val;
        }

        #endregion
    }
}

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
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
            get => _Minimum;
            set => SetProperty(ref _Minimum, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private T _Minimum;

        /// <summary>
        /// Range maximum value
        /// </summary>
        public T Maximum
        {
            get => _Maximum;
            set => SetProperty(ref _Maximum, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private T _Maximum;

        /// <summary>
        /// Range current value
        /// </summary>
        public T Value
        {
            get => _Value;
            set => SetProperty(ref _Value, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private T _Value;

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
            _Minimum = min;
            _Maximum = max;
        }

        /// <summary>
        /// Constructs a range descriptor
        /// </summary>
        public Range(T min, T max, T val) :
            this(min, max)
        {
            _Value = val;
        }

        #endregion
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;

namespace eduVPN
{
    /// <summary>
    /// Helper class to create an equality comparer inline
    /// </summary>
    /// <typeparam name="T">Object type to compare</typeparam>
    /// <see cref="https://stackoverflow.com/a/10720211/2071884"/>
    public class EqualityComparer<T> : IEqualityComparer<T>
    {
        #region Properties

        /// <summary>
        /// Function to compare two objects for equality
        /// </summary>
        public Func<T, T, bool> Compare { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs the comparer
        /// </summary>
        /// <param name="cmp">Function to compare two objects for equality</param>
        public EqualityComparer(Func<T, T, bool> cmp)
        {
            Compare = cmp;
        }

        #endregion

        #region IEqualityComparer Support

        public bool Equals(T x, T y)
        {
            return Compare(x, y);
        }

        public int GetHashCode(T obj)
        {
            return 0;
        }

        #endregion
    }
}

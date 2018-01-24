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
    /// <remarks>
    /// Based on this <a href="https://stackoverflow.com/a/10720211/2071884">example</a>.
    /// </remarks>
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

        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">The first object of type <c>T</c> to compare.</param>
        /// <param name="y">The second object of type <c>T</c> to compare.</param>
        /// <returns><c>true</c> if the specified objects are equal; <c>false</c> otherwise</returns>
        public bool Equals(T x, T y)
        {
            return Compare(x, y);
        }

        /// <summary>
        /// Returns a hash code for the specified object.
        /// </summary>
        /// <param name="obj">The object of type <c>T</c> for which a hash code is to be returned.</param>
        /// <returns>This class does not support object hashing for simplicity, therefore this method always returns <c>0</c>.</returns>
        public int GetHashCode(T obj)
        {
            return 0;
        }

        #endregion
    }
}

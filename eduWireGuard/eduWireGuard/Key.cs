/*
    eduWireGuard - WireGuard Tunnel Manager Library for eduVPN

    Copyright: 2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Diagnostics;

namespace eduWireGuard
{
    /// <summary>
    /// WireGuard key
    /// </summary>
    public class Key : IDisposable
    {
        #region Properties

        public byte[] Bytes
        {
            get
            {
                return _Bytes;
            }
            set
            {
                if (value == null || value.Length != 32)
                    throw new ArgumentException("Keys must be 32 bytes");
                _Bytes = value;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private byte[] _Bytes;

        #endregion

        #region Constructors

        public Key(byte[] bytes)
        {
            Bytes = bytes;
        }

        public Key(string base64) : this(Convert.FromBase64String(base64))
        {
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return Convert.ToBase64String(_Bytes);
        }

        #endregion

        #region IDisposable Support
        /// <summary>
        /// Flag to detect redundant <see cref="Dispose(bool)"/> calls.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool disposedValue = false;

        /// <summary>
        /// Called to dispose the object.
        /// </summary>
        /// <param name="disposing">Dispose managed objects</param>
        /// <remarks>
        /// To release resources for inherited classes, override this method.
        /// Call <c>base.Dispose(disposing)</c> within it to release parent class resources, and release child class resources if <paramref name="disposing"/> parameter is <c>true</c>.
        /// This method can get called multiple times for the same object instance. When the child specific resources should be released only once, introduce a flag to detect redundant calls.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_Bytes != null)
                        Array.Clear(_Bytes, 0, 32);
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="Dispose(bool)"/> with <c>disposing</c> parameter set to <c>true</c>.
        /// To implement resource releasing override the <see cref="Dispose(bool)"/> method.
        /// </remarks>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}

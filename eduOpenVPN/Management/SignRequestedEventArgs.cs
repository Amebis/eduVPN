/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduOpenVPN.Management
{
    /// <summary>
    /// <see cref="Session.SignRequested"/> event arguments
    /// </summary>
    public class SignRequestedEventArgs : EventArgs
    {
        #region Fields

        /// <summary>
        /// Data to be signed
        /// </summary>
        public readonly byte[] Data;

        /// <summary>
        /// Signing and padding algorithm
        /// </summary>
        public readonly SignAlgorithmType Algorithm;

        /// <summary>
        /// Signature of <see cref="Data"/> property
        /// </summary>
        public byte[] Signature;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an event arguments
        /// </summary>
        /// <param name="data">Data to be signed</param>
        /// <param name="algorithm">Signing and padding algorithm</param>
        public SignRequestedEventArgs(byte[] data, SignAlgorithmType algorithm)
        {
            Data = data;
            Algorithm = algorithm;
        }

        #endregion
    }
}

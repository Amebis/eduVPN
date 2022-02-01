/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduVPN.Xml
{
    public class MinisignPublicKey
    {
        #region Data Types

        /// <summary>
        /// Minisign algorithm mask
        /// <remarks>
        /// <a href="https://jedisct1.github.io/minisign/#signature-format">Minisign Signature format</a>
        /// </remarks>
        /// </summary>
        [Flags]

        public enum AlgorithmMask
        {
            /// <summary>
            /// All signatures
            /// </summary>
            All = Legacy | Hashed,

            /// <summary>
            /// Legacy signatures
            /// </summary>
            Legacy = 1 << 0, // 1

            /// <summary>
            /// Hashed signatures
            /// </summary>
            Hashed = 1 << 1, // 2
        }

        #endregion

        #region Properties

        /// <summary>
        /// Public key
        /// </summary>
        public byte[] Value { get; set; }

        /// <summary>
        /// Which signature algorithms are supported
        /// </summary>
        public AlgorithmMask SupportedAlgorithms { get; set; }

        #endregion
    }
}

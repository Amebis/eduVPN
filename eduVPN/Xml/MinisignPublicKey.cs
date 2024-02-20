/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
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
        /// Raw public key data
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// Which signature algorithms are supported
        /// </summary>
        public AlgorithmMask SupportedAlgorithms { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs new Minisign public key
        /// </summary>
        /// <param name="data">Minisign public key</param>
        /// <param name="supportedAlgorithms">Bitmask specifying acceptable/trusted signature algorithms</param>
        public MinisignPublicKey(byte[] data, AlgorithmMask supportedAlgorithms = MinisignPublicKey.AlgorithmMask.All)
        {
            Data = data;
            SupportedAlgorithms = supportedAlgorithms;
        }

        /// <summary>
        /// Constructs new Minisign public key
        /// </summary>
        /// <param name="base64">Base64 encoded Minisign public key</param>
        /// <param name="supportedAlgorithms">Bitmask specifying acceptable/trusted signature algorithms</param>
        public MinisignPublicKey(string base64, AlgorithmMask supportedAlgorithms = MinisignPublicKey.AlgorithmMask.All) :
            this(Convert.FromBase64String(base64), supportedAlgorithms)
        {}

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null || GetType() != obj.GetType())
                return false;
            var other = obj as MinisignPublicKey;
            if (Data.Length != other.Data.Length)
                return false;
            int diff = 0;
            for (int i = 0; i < Data.Length; i++)
                diff |= Data[i] ^ other.Data[i];
            return diff == 0;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }

        #endregion
    }
}

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.IO;

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
        /// Public key ID
        /// </summary>
        public ulong KeyId
        {
            get
            {
                using (var s = new MemoryStream(Data, false))
                using (var r = new BinaryReader(s))
                {
                    if (r.ReadChar() != 'E' || r.ReadChar() != 'd')
                        throw new ArgumentException(Resources.Strings.ErrorUnsupportedMinisignPublicKey);
                    return r.ReadUInt64();
                }
            }
        }

        /// <summary>
        /// Public key
        /// </summary>
        public byte[] Value
        {
            get
            {
                {
                    using (var s = new MemoryStream(Data, false))
                    using (var r = new BinaryReader(s))
                    {
                        if (r.ReadChar() != 'E' || r.ReadChar() != 'd')
                            throw new ArgumentException(Resources.Strings.ErrorUnsupportedMinisignPublicKey);
                        r.ReadUInt64();
                        var value = new byte[32];
                        if (r.Read(value, 0, 32) != 32)
                            throw new ArgumentException(Resources.Strings.ErrorInvalidMinisignPublicKey);
                        return value;
                    }
                }
            }
        }

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
    }
}

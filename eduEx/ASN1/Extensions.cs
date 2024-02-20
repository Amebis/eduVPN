﻿/*
    eduEx - Extensions for .NET

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace eduEx.ASN1
{
    /// <summary>
    /// <see cref="IO"/> namespace extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Reads data length from ASN.1 data stream
        /// </summary>
        /// <param name="reader">Stream of ASN.1 data</param>
        /// <returns>Length of data in bytes</returns>
        public static int ReadASN1Length(this BinaryReader reader)
        {
            var b = reader.ReadByte();
            switch (b)
            {
                case 0x81: return reader.ReadByte();
                case 0x82: return (ushort)IPAddress.NetworkToHostOrder((short)reader.ReadUInt16());
                case 0x84: return IPAddress.NetworkToHostOrder((int)reader.ReadUInt32());
                default: return b;
            }
        }

        /// <summary>
        /// Reads integer from ASN.1 data stream
        /// </summary>
        /// <param name="reader">Stream of ASN.1 data</param>
        /// <returns>Raw integer data (big-endian)</returns>
        public static byte[] ReadASN1Integer(this BinaryReader reader)
        {
            if (reader.ReadByte() != 0x02)
                throw new InvalidDataException();

            // Read length.
            var length = reader.ReadASN1Length();
            var dataEnd = length + reader.BaseStream.Position;

            try
            {
                // Read data.
                return reader.ReadBytes(length);
            }
            finally
            {
                // Make stream consistent by seeking to the end of the record.
                reader.BaseStream.Seek(dataEnd, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Reads integer from ASN.1 data stream
        /// </summary>
        /// <param name="reader">Stream of ASN.1 data</param>
        /// <returns>Integer value</returns>
        /// <exception cref="ArgumentException">Zero-width integer</exception>
        /// <exception cref="ArgumentOutOfRangeException">Integer too wide to fit in <see cref="int"/></exception>
        public static int ReadASN1IntegerInt(this BinaryReader reader)
        {
            var data = reader.ReadASN1Integer();
            var length = data.Length;
            if (length < 1)
                throw new ArgumentException();

            // Skip padding.
            var i = 0;
            var isPositive = data[i] < 0x80;
            var padding = isPositive ? (byte)0x00 : (byte)0xff;
            for (; ; i++)
            {
                if (i >= length)
                    return isPositive ? 0 : -1;
                else if (data[i] != padding)
                    break;
            }

            if (i + 4 >= length)
                throw new ArgumentOutOfRangeException();

            // Parse integer.
            var value = isPositive ? data[i] : data[i] - 0x100;
            for (i++; i < length; i++)
                value = value * 0x100 + data[i];

            return value;
        }

        /// <summary>
        /// Reads object identifier from ASN.1 data stream
        /// </summary>
        /// <param name="reader">Stream of ASN.1 data</param>
        /// <returns>Object identifier</returns>
        public static Oid ReadASN1ObjectId(this BinaryReader reader)
        {
            // OBJECT IDENTIFIER
            if (reader.ReadByte() != 0x06)
                throw new InvalidDataException();

            // Read length (should be minimum 1B).
            var length = reader.ReadASN1Length();
            if (length < 1)
                throw new ArgumentOutOfRangeException(nameof(length));
            var dataEnd = length + reader.BaseStream.Position;

            try
            {
                var sb = new StringBuilder();
                uint n = 0, bits = 0;

                while (length > 0)
                {
                    var v = reader.ReadByte(); length--;
                    n = (n << 7) | (v & (uint)0x7F);
                    bits += 7;
                    if ((v & 0x80) == 0)
                    {
                        if (sb.Length == 0)
                        {
                            var m = n < 80 ? n < 40 ? 0 : 1 : 2;
                            sb.AppendFormat("{0:D}.{1:D}", m, n - m * 40);
                        }
                        else
                            sb.AppendFormat(".{0:D}", n);

                        n = 0;
                        bits = 0;
                    }
                }
                if (bits > 0)
                    sb.Append(".incomplete");

                return new Oid(sb.ToString());
            }
            finally
            {
                // Make stream consistent by seeking to the end of the record.
                reader.BaseStream.Seek(dataEnd, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Reads RSA private key from ASN.1 data stream (PKCS#1)
        /// </summary>
        /// <param name="reader">Stream of ASN.1 data</param>
        /// <returns>Private key parameters</returns>
        public static RSAParameters ReadASN1RSAPrivateKey(this BinaryReader reader)
        {
            // SEQUENCE(RSAPrivateKey)
            if (reader.ReadByte() != 0x30)
                throw new InvalidDataException();
            var dataEnd = reader.ReadASN1Length() + reader.BaseStream.Position;

            try
            {
                // INTEGER(Version)
                if (reader.ReadASN1IntegerInt() != 0)
                    throw new InvalidDataException();

                var rsa = new RSAParameters()
                {
                    Modulus = TrimPositivePadding(reader.ReadASN1Integer()),
                    Exponent = TrimPositivePadding(reader.ReadASN1Integer()),
                    D = TrimPositivePadding(reader.ReadASN1Integer()),
                    P = TrimPositivePadding(reader.ReadASN1Integer()),
                    Q = TrimPositivePadding(reader.ReadASN1Integer()),
                    DP = TrimPositivePadding(reader.ReadASN1Integer()),
                    DQ = TrimPositivePadding(reader.ReadASN1Integer()),
                    InverseQ = TrimPositivePadding(reader.ReadASN1Integer()),
                };

                // .NET does not like PKCS padding. However, it still requires RSA parameter lengths to be in sync.
                var length = Math.Max(rsa.Modulus.Length / 2, Math.Max(rsa.D.Length / 2, Math.Max(rsa.P.Length, Math.Max(rsa.Q.Length, Math.Max(rsa.DP.Length, Math.Max(rsa.DQ.Length, rsa.InverseQ.Length))))));
                rsa.Modulus = AddPositivePadding(rsa.Modulus, length * 2);
                rsa.D = AddPositivePadding(rsa.D, length * 2);
                rsa.P = AddPositivePadding(rsa.P, length);
                rsa.Q = AddPositivePadding(rsa.Q, length);
                rsa.DP = AddPositivePadding(rsa.DP, length);
                rsa.DQ = AddPositivePadding(rsa.DQ, length);
                rsa.InverseQ = AddPositivePadding(rsa.InverseQ, length);

                return rsa;
            }
            finally
            {
                // Make stream consistent by seeking to the end of the record.
                reader.BaseStream.Seek(dataEnd, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Removes leading zero padding
        /// </summary>
        /// <param name="data">Raw integer data (big-endian)</param>
        /// <returns>Raw unsigned integer data (big-endian)</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="data"/> must be at least one byte wide.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="data"/> is negative.</exception>
        private static byte[] TrimPositivePadding(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var length = data.Length;
            if (length < 1)
                throw new ArgumentException(nameof(data));
            if (data[0] >= 0x80)
                throw new ArgumentOutOfRangeException(nameof(data));

            for (var i = 0; ; i++)
            {
                if (i + 1 >= length || data[i] != 0x00)
                {
                    // Strip the leading zero(s).
                    var finalLength = length - i;
                    var finalData = new byte[finalLength];
                    Array.Copy(data, i, finalData, 0, finalLength);
                    return finalData;
                }
            }
        }

        /// <summary>
        /// Inserts leading zero padding
        /// </summary>
        /// <param name="data">Raw unsigned integer data (big-endian)</param>
        /// <param name="finalLength">Required width</param>
        /// <returns>Raw unsigned integer data (big-endian)</returns>
        private static byte[] AddPositivePadding(byte[] data, int finalLength)
        {
            var length = data.Length;
            if (length < finalLength)
            {
                // Add leading zero(s).
                var finalData = new byte[finalLength];
                Array.Copy(data, 0, finalData, finalLength - length, length);
                data = finalData;
            }

            return data;
        }
    }
}

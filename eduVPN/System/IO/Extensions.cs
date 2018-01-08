/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace System.IO
{
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
                case 0x81: return                                             reader.ReadByte();
                case 0x82: return (ushort)IPAddress.NetworkToHostOrder((short)reader.ReadUInt16());
                case 0x84: return         IPAddress.NetworkToHostOrder((int  )reader.ReadUInt32());
                default  : return b;
            }
        }

        /// <summary>
        /// Reads integer from ASN.1 data stream
        /// </summary>
        /// <param name="reader">Stream of ASN.1 data</param>
        /// <returns>Raw integer data (big-endian)</returns>
        public static byte[] ReadASN1RawInteger(this BinaryReader reader)
        {
            if (reader.ReadByte() != 0x02)
                throw new InvalidDataException();

            // Read length.
            var length = reader.ReadASN1Length();
            long data_end = length + reader.BaseStream.Position;

            try
            {
                // Read data.
                return reader.ReadBytes(length);
            }
            finally
            {
                // Make stream consistent by seeking to the end of the record.
                reader.BaseStream.Seek(data_end, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Reads integer from ASN.1 data stream
        /// </summary>
        /// <param name="reader">Stream of ASN.1 data</param>
        /// <returns>Integer value</returns>
        public static long ReadASN1Integer(this BinaryReader reader)
        {
            var data = reader.ReadASN1RawInteger();
            var length = data.Length;
            if (length < 1)
                throw new ArgumentOutOfRangeException(nameof(length));

            // Skip padding.
            var i = 0;
            var is_positive = data[i] < 0x80;
            var padding = is_positive ? (byte)0x00 : (byte)0xff;
            for (; ; i++)
            {
                if (i >= length)
                    return is_positive ? 0 : -1;
                else if (data[i] != padding)
                    break;
            }

            // Parse integer.
            long value = is_positive ? data[i] : (long)data[i] - 0x100;
            for (i++; i < length; i++)
                value = value * 0x100 + data[i];

            return value;
        }

        /// <summary>
        /// Reads object ID from ASN.1 data stream
        /// </summary>
        /// <param name="reader">Stream of ASN.1 data</param>
        /// <returns>Object ID</returns>
        public static Oid ReadASN1ObjectID(this BinaryReader reader)
        {
            // OBJECT IDENTIFIER
            if (reader.ReadByte() != 0x06)
                throw new InvalidDataException();

            // Read length (should be minimum 1B).
            var length = reader.ReadASN1Length();
            if (length < 1)
                throw new ArgumentOutOfRangeException(nameof(length));
            long data_end = length + reader.BaseStream.Position;

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
                reader.BaseStream.Seek(data_end, SeekOrigin.Begin);
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
            long data_end = reader.ReadASN1Length() + reader.BaseStream.Position;

            try
            {
                // INTEGER(Version)
                if (reader.ReadASN1Integer() != 0)
                    throw new InvalidDataException();

                var rsa = new RSAParameters()
                {
                    Modulus = TrimPositivePadding(reader.ReadASN1RawInteger()),
                    Exponent = TrimPositivePadding(reader.ReadASN1RawInteger()),
                    D = TrimPositivePadding(reader.ReadASN1RawInteger()),
                    P = TrimPositivePadding(reader.ReadASN1RawInteger()),
                    Q = TrimPositivePadding(reader.ReadASN1RawInteger()),
                    DP = TrimPositivePadding(reader.ReadASN1RawInteger()),
                    DQ = TrimPositivePadding(reader.ReadASN1RawInteger()),
                    InverseQ = TrimPositivePadding(reader.ReadASN1RawInteger()),
                };

                // .NET does not like PKCS padding. However, it still requires RSA parameter lengths to be in sync.
                int length = Math.Max(rsa.Modulus.Length/2, Math.Max(rsa.D.Length/2, Math.Max(rsa.P.Length, Math.Max(rsa.Q.Length, Math.Max(rsa.DP.Length, Math.Max(rsa.DQ.Length, rsa.InverseQ.Length))))));
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
                reader.BaseStream.Seek(data_end, SeekOrigin.Begin);
            }
        }

        private static byte[] TrimPositivePadding(byte[] data)
        {
            var length = data.Length;
            for (var i = 0; ; i++)
            {
                if (i >= length)
                    return new byte[0];
                else if (data[i] != 0x00)
                {
                    // Strip the leading zero(s).
                    var length_final = length - i;
                    var data_final = new byte[length_final];
                    Array.Copy(data, i, data_final, 0, length_final);
                    return data_final;
                }
            }
        }

        private static byte[] AddPositivePadding(byte[] data, int length_final)
        {
            var length = data.Length;
            if (length < length_final)
            {
                // Add leading zero(s).
                var data_final = new byte[length_final];
                Array.Copy(data, 0, data_final, length_final - length, length);
                data = data_final;
            }

            return data;
        }
    }
}

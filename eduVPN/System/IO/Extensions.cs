/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Linq;
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
        /// <returns>Integer data (big-endian)</returns>
        public static byte[] ReadASN1Integer(this BinaryReader reader)
        {
            if (reader.ReadByte() != 0x02)
                throw new InvalidDataException();

            // Read length.
            var length = reader.ReadASN1Length();
            long data_end = length + reader.BaseStream.Position;

            try
            {
                // Read data.
                var data = reader.ReadBytes(length);

                if (length > 1 && data[0] == 0)
                {
                    // Strip the leading zero.
                    var data_final = new byte[length - 1];
                    Array.Copy(data, 1, data_final, 0, length - 1);
                    return data_final;
                }
                else
                    return data;
            }
            finally
            {
                // Make stream consistent by seeking to the end of the record.
                reader.BaseStream.Seek(data_end, SeekOrigin.Begin);
            }
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
                throw new ArgumentOutOfRangeException("length");
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
                if (!reader.ReadASN1Integer().SequenceEqual(new byte[] { 0 }))
                    throw new InvalidDataException();

                return new RSAParameters()
                {
                    Modulus = reader.ReadASN1Integer(),
                    Exponent = reader.ReadASN1Integer(),
                    D = reader.ReadASN1Integer(),
                    P = reader.ReadASN1Integer(),
                    Q = reader.ReadASN1Integer(),
                    DP = reader.ReadASN1Integer(),
                    DQ = reader.ReadASN1Integer(),
                    InverseQ = reader.ReadASN1Integer(),
                };
            }
            finally
            {
                // Make stream consistent by seeking to the end of the record.
                reader.BaseStream.Seek(data_end, SeekOrigin.Begin);
            }
        }
    }
}

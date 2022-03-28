/*
    eduWireGuard - WireGuard Tunnel Manager Library for eduVPN

    Copyright: 2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace eduWireGuard
{
    /// <summary>
    /// IP address with CIDR
    /// </summary>
    public class IPPrefix
    {
        #region Properties

        /// <summary>
        /// IP address
        /// </summary>
        public IPAddress Address { get; set; }

        /// <summary>
        /// CIDR of allowed IPs
        /// </summary>
        public byte CIDR { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an object from stream
        /// </summary>
        /// <param name="reader">Input stream at the location where WIREGUARD_ALLOWED_IP is written</param>
        public IPPrefix(BinaryReader reader)
        {
            var ip6 = reader.ReadBytes(16);
            var family = (AddressFamily)reader.ReadUInt16();
            switch (family)
            {
                case AddressFamily.InterNetwork:
                    var ip4 = new byte[4];
                    Array.Copy(ip6, ip4, 4);
                    Address = new IPAddress(ip4);
                    break;
                case AddressFamily.InterNetworkV6:
                    Address = new IPAddress(ip6);
                    break;
                default:
                    throw new ArgumentException("Unknown address family");
            }
            CIDR = reader.ReadByte();
            reader.ReadByte();
            reader.ReadUInt32();
        }

        /// <summary>
        /// Constructs an object from string
        /// </summary>
        /// <param name="str">String representation in IP or IP/CIDR form</param>
        public IPPrefix(string str)
        {
            var c = str.Split('/');
            switch (c.Length)
            {
                case 1:
                    Address = IPAddress.Parse(c[0].Trim());
                    switch (Address.AddressFamily)
                    {
                        case AddressFamily.InterNetwork:
                            CIDR = 32;
                            break;
                        case AddressFamily.InterNetworkV6:
                            CIDR = 128;
                            break;
                        default:
                            throw new ArgumentException("Unknown address family");
                    }
                    break;

                case 2:
                    Address = IPAddress.Parse(c[0].Trim());
                    CIDR = byte.Parse(c[1].Trim());
                    switch (Address.AddressFamily)
                    {
                        case AddressFamily.InterNetwork:
                            if (CIDR > 32)
                                throw new ArgumentException("IPv4 CIDR must not exceed 32");
                            break;
                        case AddressFamily.InterNetworkV6:
                            if (CIDR > 128)
                                throw new ArgumentException("IPv6 CIDR must not exceed 128");
                            break;
                        default:
                            throw new ArgumentException("Unknown address family");
                    }
                    break;

                default:
                    throw new ArgumentException("Unsupported IP prefix notation");
            }
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            if (Address == null)
                return "null";
            switch (Address.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    return CIDR < 32 ? Address.ToString() + "/" + CIDR.ToString() : Address.ToString();
                case AddressFamily.InterNetworkV6:
                    return CIDR < 128 ? Address.ToString() + "/" + CIDR.ToString() : Address.ToString();
            }
            return Address.ToString() + "/" + CIDR.ToString();
        }

        /// <summary>
        /// Parses string
        /// </summary>
        /// <param name="str">String representation in IP or IP/CIDR form</param>
        /// <returns>IP prefix</returns>
        public static IPPrefix Parse(string str)
        {
            return new IPPrefix(str);
        }

        /// <summary>
        /// Parses string
        /// </summary>
        /// <param name="str">String representation in IP or IP/CIDR form</param>
        /// <param name="val">IP prefix</param>
        /// <returns><c>true</c> when <paramref name="str"/> parses successfuly; <c>false</c> otherwise</returns>
        public static bool TryParse(string str, out IPPrefix val)
        {
            try
            {
                val = new IPPrefix(str);
                return true;
            }
            catch (ArgumentException)
            {
                val = null;
                return false;
            }
        }

        #endregion
    }
}

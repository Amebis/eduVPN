/*
    eduWireGuard - WireGuard Tunnel Manager Library for eduVPN

    Copyright: 2022-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace eduWireGuard
{
    /// <summary>
    /// WireGuard peer configuration
    /// </summary>
    public class Peer
    {
        #region Data Types

        /// <summary>
        /// Peer flags
        /// </summary>
        [Flags]
        private enum Flags
        {
            /// <summary>
            /// The PublicKey field is set
            /// </summary>
            HasPublicKey = 1 << 0,

            /// <summary>
            /// The PresharedKey field is set
            /// </summary>
            HasPresharedKey = 1 << 1,

            /// <summary>
            /// The PersistentKeepAlive field is set
            /// </summary>
            HasPersistentKeepalive = 1 << 2,

            /// <summary>
            /// The Endpoint field is set
            /// </summary>
            HasEndpoint = 1 << 3,

            /// <summary>
            /// Remove all allowed IPs before adding new ones
            /// </summary>
            ReplaceAllowedIPs = 1 << 5,

            /// <summary>
            /// Remove specified peer
            /// </summary>
            Remove = 1 << 6,

            /// <summary>
            /// Do not add a new peer
            /// </summary>
            UpdateOnly = 1 << 7
        }

        #endregion

        #region Properties

        /// <summary>
        /// Public key, the peer's primary identifier
        /// </summary>
        public Key PublicKey { get; set; }

        /// <summary>
        /// Preshared key for additional layer of post-quantum resistance
        /// </summary>
        public Key PresharedKey { get; set; }

        /// <summary>
        /// Seconds interval, or 0 to disable
        /// </summary>
        public ushort PersistentKeepalive { get; set; }

        /// <summary>
        /// Endpoint, with IP address and UDP port number
        /// </summary>
        public IPEndPoint Endpoint { get; set; }

        /// <summary>
        /// Number of bytes transmitted
        /// </summary>
        public ulong TxBytes { get; set; }

        /// <summary>
        /// Number of bytes received
        /// </summary>
        public ulong RxBytes { get; set; }

        /// <summary>
        /// Time of the last handshake, in 100ns intervals since 1601-01-01 UTC; or <see cref="DateTimeOffset.MinValue"/> if unknown
        /// </summary>
        public DateTimeOffset LastHandshake { get; set; }

        /// <summary>
        /// Allowed IP addresses
        /// </summary>
        public List<IPPrefix> AllowedIPs { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an object
        /// </summary>
        public Peer()
        { }

        /// <summary>
        /// Constructs an object from stream
        /// </summary>
        /// <param name="reader">Input stream at the location where WIREGUARD_PEER is written</param>
        public Peer(BinaryReader reader)
        {
            var flags = (Flags)reader.ReadUInt32();
            reader.ReadUInt32();
            var key = reader.ReadBytes(32);
            if ((flags & Flags.HasPublicKey) != 0)
                PublicKey = new Key(key);
            key = reader.ReadBytes(32);
            if ((flags & Flags.HasPresharedKey) != 0)
                PresharedKey = new Key(key);
            var persistentKeepalive = reader.ReadUInt16();
            reader.ReadUInt16();
            if ((flags & Flags.HasPersistentKeepalive) != 0)
                PersistentKeepalive = persistentKeepalive;
            if ((flags & Flags.HasEndpoint) != 0)
            {
                var family = (AddressFamily)reader.ReadUInt16();
                var port = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
                switch (family)
                {
                    case AddressFamily.InterNetwork:
                        var ip4 = reader.ReadBytes(4);
                        reader.ReadBytes(20);
                        Endpoint = new IPEndPoint(new IPAddress(ip4), port);
                        break;
                    case AddressFamily.InterNetworkV6:
                        reader.ReadUInt32();
                        var ip6 = reader.ReadBytes(16);
                        var scopeId = reader.ReadUInt32();
                        Endpoint = new IPEndPoint(new IPAddress(ip6, scopeId), port);
                        break;
                    default:
                        throw new ArgumentException("Unknown endpoint address family");
                }
            }
            else
                reader.ReadBytes(28);
            TxBytes = reader.ReadUInt64();
            RxBytes = reader.ReadUInt64();
            var lastHandshake = reader.ReadInt64();
            if (lastHandshake != 0)
                LastHandshake = DateTime.FromFileTimeUtc(lastHandshake);
            var allowedIPsCount = reader.ReadUInt32();
            reader.ReadUInt32();
            AllowedIPs = new List<IPPrefix>((int)allowedIPsCount);
            for (uint i = 0; i < allowedIPsCount; ++i)
                AllowedIPs.Add(new IPPrefix(reader));
        }

        #endregion
    }
}

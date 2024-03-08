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
    /// WireGuard interface configuration
    /// </summary>
    public class Interface
    {
        #region Data Types

        /// <summary>
        /// Interface flags
        /// </summary>
        [Flags]
        private enum Flags
        {
            /// <summary>
            /// The PublicKey field is set
            /// </summary>
            HasPublicKey = 1 << 0,

            /// <summary>
            /// The PrivateKey field is set
            /// </summary>
            HasPrivateKey = 1 << 1,

            /// <summary>
            /// The ListenPort field is set
            /// </summary>
            HasListenPort = 1 << 2,

            /// <summary>
            /// Remove all peers before adding new ones
            /// </summary>
            ReplacePeers = 1 << 3
        }

        /// <summary>
        /// Parser state
        /// </summary>
        private enum ParserState
        {
            InInterfaceSection,
            InPeerSection,
            NotInASection,
        }

        #endregion

        #region Properties

        /// <summary>
        /// Private key of interface
        /// </summary>
        public Key PrivateKey { get; set; }

        /// <summary>
        /// Corresponding public key of private key
        /// </summary>
        public Key PublicKey { get; set; }

        /// <summary>
        /// Port for UDP listen socket, or 0 to choose randomly
        /// </summary>
        public ushort ListenPort { get; set; }

        /// <summary>
        /// Maximum transfer unit
        /// </summary>
        public ushort MTU { get; set; }

        /// <summary>
        /// Interface addresses
        /// </summary>
        public List<IPPrefix> Addresses { get; set; }

        /// <summary>
        /// DNS servers
        /// </summary>
        public List<IPAddress> DNS { get; set; }

        /// <summary>
        /// DNS search domains
        /// </summary>
        public List<string> DNSSearch { get; set; }

        /// <summary>
        /// Pre-up command
        /// </summary>
        public string PreUp { get; set; }

        /// <summary>
        /// Post-up command
        /// </summary>
        public string PostUp { get; set; }

        /// <summary>
        /// Pre-down command
        /// </summary>
        public string PreDown { get; set; }

        /// <summary>
        /// Post-down command
        /// </summary>
        public string PostDown { get; set; }

        /// <summary>
        /// Is route management disabled?
        /// </summary>
        public bool TableOff { get; set; }

        /// <summary>
        /// Peers
        /// </summary>
        public List<Peer> Peers { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an object from stream
        /// </summary>
        /// <param name="reader">Input stream at the location where WIREGUARD_PEER is written</param>
        public Interface(BinaryReader reader)
        {
            var flags = (Flags)reader.ReadUInt32();
            var listenPort = reader.ReadUInt16();
            if ((flags & Flags.HasListenPort) != 0)
                ListenPort = listenPort;
            var key = reader.ReadBytes(32);
            if ((flags & Flags.HasPrivateKey) != 0)
                PrivateKey = new Key(key);
            key = reader.ReadBytes(32);
            reader.ReadUInt16();
            if ((flags & Flags.HasPublicKey) != 0)
                PublicKey = new Key(key);
            var peersCount = reader.ReadUInt32();
            reader.ReadUInt32();
            Peers = new List<Peer>((int)peersCount);
            for (uint i = 0; i < peersCount; ++i)
                Peers.Add(new Peer(reader));
        }

        /// <summary>
        /// Constructs an object from wg-quick stream
        /// </summary>
        /// <param name="reader">Input stream</param>
        public Interface(TextReader reader)
        {
            var parserState = ParserState.NotInASection;
            Peers = new List<Peer>();
            Peer peer = null;
            for (; ; )
            {
                var line = reader.ReadLine();
                if (line == null)
                    break;
                int index = line.IndexOf("#");
                if (index >= 0)
                    line = line.Substring(0, index);
                line = line.Trim();
                if (line.Length == 0)
                    continue;
                var lineLower = line.ToLowerInvariant();

                if (lineLower == "[interface]")
                {
                    if (peer != null)
                        Peers.Add(peer);
                    parserState = ParserState.InInterfaceSection;
                    continue;
                }
                if (lineLower == "[peer]")
                {
                    if (peer != null)
                        Peers.Add(peer);
                    peer = new Peer();
                    parserState = ParserState.InPeerSection;
                    continue;
                }
                if (parserState == ParserState.NotInASection)
                    throw new ArgumentException("Line must occur in a section: " + line);

                index = line.IndexOf('=');
                if (index < 0)
                    throw new ArgumentException("Config key is missing an equals separator: " + line);
                var key = lineLower.Substring(0, index).TrimEnd();
                var val = line.Substring(index + 1, line.Length - (index + 1)).TrimStart();
                if (val.Length == 0)
                    throw new ArgumentException("Key must have a value: " + line);

                if (parserState == ParserState.InInterfaceSection)
                {
                    switch (key)
                    {
                        case "privatekey":
                            PrivateKey = ParseKeyBase64(val);
                            break;

                        case "listenport":
                            ListenPort = ParsePort(val);
                            break;

                        case "mtu":
                            MTU = ParseMTU(val);
                            break;

                        case "address":
                            var addresses = SplitList(val);
                            Addresses = new List<IPPrefix>(addresses.Length);
                            for (var i = 0; i < addresses.Length; ++i)
                                Addresses.Add(ParseIPCidr(addresses[i]));
                            break;

                        case "dns":
                            addresses = SplitList(val);
                            var DNS = new List<IPAddress>();
                            var DNSSearch = new List<string>();
                            foreach (var a in addresses)
                            {
                                if (IPAddress.TryParse(a, out var address))
                                    DNS.Add(address);
                                else
                                    DNSSearch.Add(a);
                            }
                            break;

                        case "preup":
                            PreUp = val;
                            break;

                        case "postup":
                            PostUp = val;
                            break;

                        case "predown":
                            PreDown = val;
                            break;

                        case "postdown":
                            PostDown = val;
                            break;

                        case "table":
                            TableOff = ParseTableOff(val);
                            break;

                        default:
                            throw new ArgumentException("Invalid key for [Interface] section: " + key);
                    }
                }
                else if (parserState == ParserState.InPeerSection)
                {
                    switch (key)
                    {
                        case "publickey":
                            peer.PublicKey = ParseKeyBase64(val);
                            break;

                        case "presharedkey":
                            peer.PresharedKey = ParseKeyBase64(val);
                            break;

                        case "allowedips":
                            var addresses = SplitList(val);
                            peer.AllowedIPs = new List<IPPrefix>(addresses.Length);
                            for (var i = 0; i < addresses.Length; ++i)
                                peer.AllowedIPs.Add(ParseIPCidr(addresses[i]));
                            break;

                        case "persistentkeepalive":
                            peer.PersistentKeepalive = ParsePersistentKeepalive(val);
                            break;

                        case "endpoint":
                            peer.Endpoint = ParseEndpoint(val);
                            break;

                        case "proxyendpoint":
                            peer.ProxyEndpoint = ParseURL(val);
                            break;

                        default:
                            throw new ArgumentException("Invalid key for [Peer] section: " + key);
                    }
                }
            }
            if (peer != null)
                Peers.Add(peer);

            if (PrivateKey == null)
                throw new ArgumentException("An interface must have a private key");
            foreach (var p in Peers)
                if (p.PublicKey == null)
                    throw new ArgumentException("All peers must have public keys");
        }

        #endregion

        #region Members

        private static IPPrefix ParseIPCidr(string s)
        {
            return IPPrefix.Parse(s);
        }

        private static Endpoint ParseEndpoint(string s)
        {
            var i = s.LastIndexOf(':');
            if (i < 0)
                throw new ArgumentException("Missing port from endpoint: " + s);
            var host = s.Substring(0, i);
            var portStr = s.Substring(i + 1);
            if (host.Length < 1)
                throw new ArgumentException("Invalid endpoint host: " + host);
            var port = ushort.Parse(portStr);
            var hostColon = host.IndexOf(':');
            if (host[0] == '[' || host[host.Length - 1] == ']' || hostColon > 0)
            {
                var err = new ArgumentException("Brackets must contain an IPv6 address: " + host);
                if (host.Length > 3 && host[0] == '[' && host[host.Length - 1] == ']' && hostColon > 0)
                {
                    var end = host.Length - 1;
                    i = host.LastIndexOf('%');
                    if (i > 1)
                        end = i;
                    var maybeV6 = IPAddress.Parse(host.Substring(1, end - 1));
                    if (maybeV6.AddressFamily != AddressFamily.InterNetworkV6)
                        throw err;
                }
                else
                    throw err;
                host = host.Substring(1, host.Length - 2);
            }
            return new Endpoint(host, port);
        }

        private static ushort ParseMTU(string s) {
            var m = long.Parse(s);
            if (m < 576 || m > 65535)
                throw new ArgumentException("Invalid MTU: " + s);
            return (ushort)m;
        }

        private static ushort ParsePort(string s) {
            var m = long.Parse(s);
            if (m < 0 || m > 65535)
                throw new ArgumentException("Invalid port: " + s);
            return (ushort)m;
        }

        private static Uri ParseURL(string s)
        {
            return new Uri(s);
        }

        private static ushort ParsePersistentKeepalive(string s)
        {
            if (s == "off")
                return 0;
            var m = long.Parse(s);
            if (m < 0 || m > 65535)
                throw new ArgumentException("Invalid persistent keepalive: " + s);
            return (ushort)m;
        }

        private static bool ParseTableOff(string s)
        {
            switch (s)
            {
                case "off": return true;
                case "auto":
                case "main": return false;
                default:
                    uint.Parse(s);
                    return false;
            }
        }

        private static Key ParseKeyBase64(string s)
        {
            return new Key(s);
        }

        private static string[] SplitList(string val)
        {
            var list = val.Split(',');
            for (var i = 0; i < list.Length; ++i)
            {
                var trim = list[i].Trim();
                if (trim.Length == 0)
                    throw new ArgumentException("Two commas in a row: " + val);
                list[i] = trim;
            }
            return list;
        }

        #endregion
    }
}

﻿/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduOpenVPN
{
    /// <summary>
    /// Unix signals used by OpenVPN
    /// </summary>
    public enum SignalType
    {
        /// <summary>
        /// Cause OpenVPN to close all TUN/TAP and network connections, restart, re-read the configuration file (if any), and reopen TUN/TAP and network connections.
        /// </summary>
        SIGHUP = 1,

        /// <summary>
        /// Causes OpenVPN to exit gracefully.
        /// </summary>
        SIGTERM = 15,

        /// <summary>
        /// <para>Like <see cref="SIGHUP"/>, except don't re-read configuration file, and possibly don't close and reopen TUN/TAP device,
        /// re-read key files, preserve local IP address/port, or preserve most recently authenticated remote IP address/port
        /// based on <c>--persist-tun</c>, <c>--persist-key</c>, <c>--persist-local-ip</c>, and <c>--persist-remote-ip</c> options
        /// respectively (see above).</para>
        ///
        /// <para>This signal may also be internally generated by a timeout condition, governed by the <c>--ping-restart</c>
        /// option.</para>
        ///
        /// <para>This signal, when combined with <c>--persist-remote-ip</c>, may be sent when the underlying parameters of the
        /// host's network interface change such as when the host is a DHCP client and is assigned a new IP address.
        /// See <c>--ipchange</c> for more information.</para>
        /// </summary>
        SIGUSR1,

        /// <summary>
        /// Causes OpenVPN to display its current statistics (to the syslog file if <c>--daemon</c> is used, or <c>stdout</c> otherwise).
        /// </summary>
        SIGUSR2,
    }
}

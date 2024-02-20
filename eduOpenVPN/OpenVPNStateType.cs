/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduOpenVPN
{
    /// <summary>
    /// OpenVPN state type
    /// </summary>
    public enum OpenVPNStateType
    {
        /// <summary>
        /// Initial, undefined state (default)
        /// </summary>
        [ParameterValue("INITIAL")]
        Initial = 0,

        /// <summary>
        /// Management interface has been initialized
        /// </summary>
        [ParameterValue("CONNECTING")]
        Connecting = 1,

        /// <summary>
        /// Assigning IP address to virtual network interface
        /// </summary>
        [ParameterValue("ASSIGN_IP")]
        AssigningIP = 2,

        /// <summary>
        /// Adding routes to system
        /// </summary>
        [ParameterValue("ADD_ROUTES")]
        AddingRoutes = 3,

        /// <summary>
        /// Initialization Sequence Completed
        /// </summary>
        [ParameterValue("CONNECTED")]
        Connected = 4,

        /// <summary>
        /// A restart has occurred
        /// </summary>
        [ParameterValue("RECONNECTING")]
        Reconnecting = 5,

        /// <summary>
        /// A graceful exit is in progress
        /// </summary>
        [ParameterValue("EXITING")]
        Exiting = 6,

        /// <summary>
        /// Waiting for initial response from server (Client only)
        /// </summary>
        [ParameterValue("WAIT")]
        Waiting = 7,

        /// <summary>
        /// Authenticating with server (Client only)
        /// </summary>
        [ParameterValue("AUTH")]
        Authenticating = 8,

        /// <summary>
        /// Downloading configuration options from server (Client only)
        /// </summary>
        [ParameterValue("GET_CONFIG")]
        GettingConfiguration = 9,

        /// <summary>
        /// DNS lookup (Client only)
        /// </summary>
        [ParameterValue("RESOLVE")]
        Resolving = 10,

        /// <summary>
        /// Connecting to TCP server (Client only)
        /// </summary>
        [ParameterValue("TCP_CONNECT")]
        TcpConnecting = 11,
    }
}

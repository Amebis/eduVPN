/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduOpenVPN.Management
{
    /// <summary>
    /// OpenVPN Management session remote "MOD" command action
    /// </summary>
    public class RemoteModAction : RemoteAction
    {
        #region Properties

        /// <summary>
        /// Hostname or IP address
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// IP port
        /// </summary>
        public int Port { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a command action
        /// </summary>
        /// <param name="host">Hostname or IP address</param>
        /// <param name="port">IP port</param>
        public RemoteModAction(string host, int port)
        {
            Host = host;
            Port = port;
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("MOD {0} {1:D}", Configuration.EscapeParamValue(Host), Port);
        }

        #endregion
    }
}

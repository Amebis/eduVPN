/*
    eduWireGuard - WireGuard Tunnel Manager Library for eduVPN

    Copyright: 2022-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduEx.Async;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace eduWireGuard.ManagerService
{
    /// <summary>
    /// WireGuard Manager Service session
    /// </summary>
    public class Session : IDisposable
    {
        #region Constants

        /// <summary>
        /// Maximum permissible WireGuard tunnel name length
        /// </summary>
        public const int MaximumTunnelNameLength = 32;

        #endregion

        #region Properties

        /// <summary>
        /// Named pipe stream to WireGuard Tunnel Manager service
        /// </summary>
        public NamedPipeClientStream Stream { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Connects to WireGuard Tunnel Manager service and sends a command to activate a tunnel
        /// </summary>
        /// <param name="pipeName">Pipe name to connect to (e.g. "eduWGManager$eduVPN")</param>
        /// <param name="tunnelName">Tunnel name</param>
        /// <param name="tunnelConfig">wg-quick tunnel config</param>
        /// <param name="timeout">The number of milliseconds to wait for the server to respond before the connection times out.</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        public void Activate(string pipeName, string tunnelName, string tunnelConfig, int timeout = 3000, CancellationToken ct = default)
        {
            try
            {
                // Connect to WireGuard Tunnel Manager service via named pipe.
                Trace.TraceInformation("Connecting to WireGuard Tunnel Manager service");
                Stream = new NamedPipeClientStream(".", pipeName);
                Stream.Connect(timeout);
                Stream.ReadMode = PipeTransmissionMode.Message;
            }
            catch (Exception ex) { throw new AggregateException(string.Format(Resources.Strings.ErrorManagerServiceConnect, pipeName), ex); }

            // Ask WireGuard Tunnel Manager service to activate the tunnel for us.
            Trace.TraceInformation("Triggering tunnel activation");
            using (var msgStream = new MemoryStream())
            using (var writer = new BinaryWriter(msgStream))
            {
                writer.Write((int)MessageCode.ActivateTunnel);

                // Tunnel name
                var tunnelName8 = Encoding.UTF8.GetBytes(tunnelName);
                if (tunnelName8.Length > MaximumTunnelNameLength)
                    throw new ArgumentException(Resources.Strings.ErrorTunnelNameTooLong, nameof(tunnelName));
                writer.Write(tunnelName8);
                for (var i = tunnelName8.Length; i < MaximumTunnelNameLength; i++)
                    writer.Write((byte)0);

                // Tunnel configuration
                var tunnelConfig8 = Encoding.UTF8.GetBytes(tunnelConfig);
                writer.Write((uint)tunnelConfig8.Length);
                writer.Write(tunnelConfig8);

                Stream.Write(msgStream.GetBuffer(), 0, (int)msgStream.Length, ct);
            }

            // Read and analyze status.
            var status = ReadStatus(ct);
            if (!status.Success)
                throw new ManagerServiceException(status.Win32Error, status.Message);
        }

        /// <summary>
        /// Sends a command to deactivate a tunnel
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        public void Deactivate(CancellationToken ct = default)
        {
            // Ask WireGuard Tunnel Manager service to deactivate the tunnel for us.
            Trace.TraceInformation("Triggering tunnel deactivation");
            using (var msgStream = new MemoryStream())
            using (var writer = new BinaryWriter(msgStream))
            {
                writer.Write((int)MessageCode.DeactivateTunnel);
                Stream.Write(msgStream.GetBuffer(), 0, (int)msgStream.Length, ct);
            }

            // Read and analyze status.
            var status = ReadStatus(ct);
            if (!status.Success)
                throw new ManagerServiceException(status.Win32Error, status.Message);
        }

        /// <summary>
        /// Retrieves current tunnel config
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns></returns>
        public Interface GetTunnelConfig(CancellationToken ct = default)
        {
            // Ask WireGuard Tunnel Manager service to fetch tunnel config for us.
            using (var msgStream = new MemoryStream())
            using (var writer = new BinaryWriter(msgStream))
            {
                writer.Write((int)MessageCode.GetTunnelConfig);
                Stream.Write(msgStream.GetBuffer(), 0, (int)msgStream.Length, ct);
            }

            // Read and analyze response.
            var data = new byte[1048576]; // Limit to 1MiB
            var count = Stream.Read(data, 0, data.Length, ct);
            try
            {
                using (var msgStream = new MemoryStream(data, 0, count, false))
                using (var reader = new BinaryReader(msgStream))
                {
                    var code = (MessageCode)reader.ReadInt32();
                    switch (code)
                    {
                        case MessageCode.TunnelConfig:
                            var countCfg = reader.ReadUInt32();
                            if (countCfg > count - 8)
                                throw new InvalidDataException();
                            return new Interface(reader);

                        case MessageCode.Status:
                            var status = new Status(reader);
                            throw new ManagerServiceException(status.Win32Error, status.Message);

                        default:
                            throw new InvalidDataException();
                    }
                }
            }
            finally
            {
                Array.Clear(data, 0, count);
            }
        }

        /// <summary>
        /// Disconnects from WireGuard Tunnel Manager service
        /// </summary>
        /// <remarks>Instead of calling this method, ensure that the connection is properly disposed.</remarks>
        public void Disconnect()
        {
            if (Stream != null)
            {
                Trace.TraceInformation("Disconnecting from WireGuard Tunnel Manager service");
                Stream.Close();
                Stream = null;
            }
        }

        /// <summary>
        /// Reads WireGuard Tunnel Manager service reported status
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>Status</returns>
        public Status ReadStatus(CancellationToken ct = default)
        {
            var data = new byte[1048576]; // Limit to 1MiB
            var count = Stream.Read(data, 0, data.Length, ct);
            using (var msgStream = new MemoryStream(data, 0, count, false))
            using (var reader = new BinaryReader(msgStream))
            {
                var code = (MessageCode)reader.ReadInt32();
                if (code != MessageCode.Status)
                    throw new InvalidDataException();
                return new Status(reader);
            }
        }

        #endregion

        #region IDisposable Support
        /// <summary>
        /// Flag to detect redundant <see cref="Dispose(bool)"/> calls.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool disposedValue = false;

        /// <summary>
        /// Called to dispose the object.
        /// </summary>
        /// <param name="disposing">Dispose managed objects</param>
        /// <remarks>
        /// To release resources for inherited classes, override this method.
        /// Call <c>base.Dispose(disposing)</c> within it to release parent class resources, and release child class resources if <paramref name="disposing"/> parameter is <c>true</c>.
        /// This method can get called multiple times for the same object instance. When the child specific resources should be released only once, introduce a flag to detect redundant calls.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Stream != null)
                        Stream.Dispose();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="Dispose(bool)"/> with <c>disposing</c> parameter set to <c>true</c>.
        /// To implement resource releasing override the <see cref="Dispose(bool)"/> method.
        /// </remarks>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}

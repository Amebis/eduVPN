/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduEx.Async;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace eduOpenVPN.InteractiveService
{
    /// <summary>
    /// OpenVPN Interactive Service session
    /// </summary>
    public class Session : IDisposable
    {
        #region Properties

        /// <summary>
        /// Named pipe stream to OpenVPN Interactive Service
        /// </summary>
        public NamedPipeClientStream Stream { get; private set; }

        /// <summary>
        /// openvpn.exe process identifier
        /// </summary>
        public int ProcessId { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Connects to OpenVPN Interactive Service and sends a command to start openvpn.exe
        /// </summary>
        /// <param name="pipeName">Pipe name to connect to (e.g. "openvpn\\service")</param>
        /// <param name="workingFolder">openvpn.exe process working folder to start in</param>
        /// <param name="arguments">openvpn.exe command line parameters</param>
        /// <param name="stdin">Text to send to openvpn.exe on start via stdin</param>
        /// <param name="timeout">The number of milliseconds to wait for the server to respond before the connection times out.</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        public void Connect(string pipeName, string workingFolder, string[] arguments, string stdin, int timeout = 3000, CancellationToken ct = default)
        {
            try
            {
                // Connect to OpenVPN Interactive Service via named pipe.
                Trace.TraceInformation("Connecting to OpenVPN interactive service");
                Stream = new NamedPipeClientStream(".", pipeName);
                Stream.Connect(timeout);
                Stream.ReadMode = PipeTransmissionMode.Message;
            }
            catch (Exception ex) { throw new AggregateException(string.Format(Resources.Strings.ErrorInteractiveServiceConnect, pipeName), ex); }

            // Ask OpenVPN Interactive Service to start openvpn.exe for us.
            Trace.TraceInformation("Triggering openvpn.exe launch");
            var encodingUtf16 = new UnicodeEncoding(false, false);
            using (var msgStream = new MemoryStream())
            using (var writer = new BinaryWriter(msgStream, encodingUtf16))
            {
                // Working folder (zero terminated)
                writer.Write(workingFolder.ToArray());
                writer.Write((char)0);

                // openvpn.exe command line parameters (zero terminated)
                writer.Write(string.Join(" ", arguments.Select(arg => arg.IndexOfAny(new char[] { ' ', '"' }) >= 0 ? "\"" + arg.Replace("\"", "\\\"") + "\"" : arg)).ToArray());
                writer.Write((char)0);

                // stdin (zero terminated)
                writer.Write(stdin.ToArray());
                writer.Write((char)0);

                Stream.Write(msgStream.GetBuffer(), 0, (int)msgStream.Length, ct);
            }

            // Read and analyze status.
            var statusTask = ReadStatusAsync();
            try { statusTask.Wait(ct); }
            catch (OperationCanceledException) { throw; }
            catch (AggregateException ex) { throw ex.InnerException; }
            ProcessId =
                statusTask.Result is StatusError statusErr && statusErr.Code != 0 ? throw new InteractiveServiceException(statusErr.Code, statusErr.Function, statusErr.Message) :
                statusTask.Result is StatusProcessId statusPid ? statusPid.ProcessId : 0;
        }

        /// <summary>
        /// Disconnects from OpenVPN Interactive Service
        /// </summary>
        /// <remarks>Instead of calling this method, ensure that the connection is properly disposed.</remarks>
        public void Disconnect()
        {
            if (Stream != null)
            {
                Trace.TraceInformation("Disconnecting from OpenVPN interactive service");
                Stream.Close();
                Stream = null;
            }
        }

        /// <summary>
        /// Reads OpenVPN Interactive reported status
        /// </summary>
        /// <returns>Status</returns>
        public async Task<Status> ReadStatusAsync()
        {
            var data = new byte[1048576]; // Limit to 1MiB
            return Status.FromResponse(new string(Encoding.Unicode.GetChars(data, 0, await Stream.ReadAsync(data, 0, data.Length))));
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

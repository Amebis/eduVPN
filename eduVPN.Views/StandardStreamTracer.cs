/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace eduVPN.Views
{
    /// <summary>
    /// Intercepts standard output and error and redirects them to Trace.TraceInformation() call
    /// </summary>
    class StandardStreamTracer : IDisposable
    {
        #region Fields

        readonly SafeFileHandle WriteHandle;
        readonly SafeFileHandle ReadHandle;
        readonly IntPtr OriginalOutputHandle;
        readonly IntPtr OriginalErrorHandle;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates standard output/error stream to trace redirector
        /// </summary>
        public StandardStreamTracer()
        {
            CreatePipe(out ReadHandle, out WriteHandle, IntPtr.Zero, 0);
            new Thread(() =>
            {
                using (var stream = new FileStream(ReadHandle, FileAccess.Read))
                using (var reader = new StreamReader(stream))
                {
                    try
                    {
                        for (; ; )
                        {
                            var line = reader.ReadLine();
                            if (line == null)
                                break;
                            Trace.TraceInformation(line);
                        }
                    }
                    catch { }
                }
            }).Start();
            OriginalOutputHandle = GetStdHandle(/*STD_OUTPUT_HANDLE*/-11);
            OriginalErrorHandle = GetStdHandle(/*STD_ERROR_HANDLE*/-12);
            IntPtr writeHandle = WriteHandle.DangerousGetHandle();
            SetStdHandle(/*STD_OUTPUT_HANDLE*/-11, writeHandle);
            SetStdHandle(/*STD_ERROR_HANDLE*/-12, writeHandle);
        }

        #endregion

        #region Methods

        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern int CreatePipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle, IntPtr sa, uint size);

        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int device);

        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern int SetStdHandle(int device, IntPtr handle);

        #endregion

        #region IDisposable Support
        /// <summary>
        /// Flag to detect redundant <see cref="Dispose(bool)"/> calls.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool disposedValue = false;

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
                    SetStdHandle(/*STD_OUTPUT_HANDLE*/-11, OriginalOutputHandle);
                    SetStdHandle(/*STD_ERROR_HANDLE*/-12, OriginalErrorHandle);
                    WriteHandle.Close();
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

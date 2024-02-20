/*
    eduEx - Extensions for .NET

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace eduEx.System.IO
{
    /// <summary>
    /// <see cref="IO"/> namespace extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Writes array of bytes to the <see cref="Stream"/>
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="buffer">An array of type <see cref="Byte"/> that contains the data to write to the <paramref name="stream"/></param>
        [DebuggerStepThrough]
        public static void Write(this Stream stream, byte[] buffer)
        {
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes array of bytes to the <see cref="Stream"/> asynchronously
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="buffer">An array of type <see cref="Byte"/> that contains the data to write to the <paramref name="stream"/></param>
        [DebuggerStepThrough]
        public static Task WriteAsync(this Stream stream, byte[] buffer)
        {
            return stream.WriteAsync(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes array of bytes to the <see cref="Stream"/> asynchronously
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="buffer">An array of type <see cref="Byte"/> that contains the data to write to the <paramref name="stream"/></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/></param>
        [DebuggerStepThrough]
        public static Task WriteAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            return stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        /// <summary>
        /// Reads array of bytes from the <see cref="Stream"/>
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="buffer">An array of type <see cref="Byte"/> that contains the data to read from the <see cref="Stream"/></param>
        [DebuggerStepThrough]
        public static int Read(this Stream stream, byte[] buffer)
        {
            return stream.Read(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Reads array of bytes from the <see cref="Stream"/> asynchronously
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="buffer">An array of type <see cref="Byte"/> that contains the data to read from the <see cref="Stream"/></param>
        [DebuggerStepThrough]
        public static Task<int> ReadAsync(this Stream stream, byte[] buffer)
        {
            return stream.ReadAsync(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Reads array of bytes from the <see cref="Stream"/> asynchronously
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="buffer">An array of type <see cref="Byte"/> that contains the data to read from the <see cref="Stream"/></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        [DebuggerStepThrough]
        public static Task<int> ReadAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            return stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        /// <summary>
        /// Writes array of bytes to the <see cref="StreamWriter"/>
        /// </summary>
        /// <param name="writer">StreamWriter</param>
        /// <param name="buffer">An array of type <see cref="char"/> that contains the data to write to the <see cref="StreamWriter"/></param>
        [DebuggerStepThrough]
        public static void Write(this StreamWriter writer, char[] buffer)
        {
            writer.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes array of bytes to the <see cref="StreamWriter"/> asynchronously
        /// </summary>
        /// <param name="writer">StreamWriter</param>
        /// <param name="buffer">An array of type <see cref="char"/> that contains the data to write to the <see cref="StreamWriter"/></param>
        [DebuggerStepThrough]
        public static Task WriteAsync(this StreamWriter writer, char[] buffer)
        {
            return writer.WriteAsync(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Reads array of bytes from the <see cref="StreamReader"/>
        /// </summary>
        /// <param name="reader">StreamReader</param>
        /// <param name="buffer">An array of type <see cref="char"/> that contains the data to read from the <see cref="StreamReader"/></param>
        [DebuggerStepThrough]
        public static int Read(this StreamReader reader, char[] buffer)
        {
            return reader.Read(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Reads array of bytes from the <see cref="StreamReader"/> asynchronously
        /// </summary>
        /// <param name="reader">StreamReader</param>
        /// <param name="buffer">An array of type <see cref="char"/> that contains the data to read from the <see cref="StreamReader"/></param>
        [DebuggerStepThrough]
        public static Task<int> ReadAsync(this StreamReader reader, char[] buffer)
        {
            return reader.ReadAsync(buffer, 0, buffer.Length);
        }
    }
}

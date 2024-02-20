/*
    eduEx - Extensions for .NET

    Copyright: 2021-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.IO;
using System.Threading;

namespace eduEx.Async
{
    public static class Extensions
    {
        /// <summary>
        /// Reads a sequence of bytes from the current stream, advances the position within the stream by the number of bytes read, and monitors cancellation requests.
        /// </summary>
        /// <param name="stream">A stream</param>
        /// <param name="buffer">The buffer to write the data into</param>
        /// <param name="offset">The byte offset in buffer at which to begin writing data from the stream</param>
        /// <param name="count">The maximum number of bytes to read</param>
        /// <param name="ct">The token to monitor for cancellation requests. The default value is System.Threading.CancellationToken.None</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        public static int Read(this Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
        {
            var task = stream.ReadAsync(buffer, offset, count, ct);
            try { task.Wait(ct); }
            catch (AggregateException ex) { throw ex.InnerException; }
            return task.Result;
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream, advances the current position within this stream by the number of bytes written, and monitors cancellation requests.
        /// </summary>
        /// <param name="stream">A stream</param>
        /// <param name="buffer">The buffer to write data from</param>
        /// <param name="offset">The zero-based byte offset in buffer from which to begin copying bytes to the stream</param>
        /// <param name="count">The maximum number of bytes to write</param>
        /// <param name="ct">The token to monitor for cancellation requests. The default value is System.Threading.CancellationToken.None</param>
        public static void Write(this Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
        {
            var task = stream.WriteAsync(buffer, offset, count, ct);
            try { task.Wait(ct); }
            catch (AggregateException ex) { throw ex.InnerException; }
        }

        /// <summary>
        /// Reads a specified maximum number of characters from the current text reader asynchronously and writes the data to a buffer, beginning at the specified index.
        /// </summary>
        /// <param name="reader">Text reader</param>
        /// <param name="buffer">When this method returns, contains the specified character array with the values between index and (index + count - 1) replaced by the characters read from the current source</param>
        /// <param name="index">The position in buffer at which to begin writing</param>
        /// <param name="count">The maximum number of characters to read. If the end of the text is reached before the specified number of characters is read into the buffer, the current method returns</param>
        /// <param name="ct">The token to monitor for cancellation requests. The default value is System.Threading.CancellationToken.None</param>
        /// <returns>The number of characters that have been read. The number will be less than or equal to count, depending on whether the data is available within the reader. This method returns 0 (zero) if it is called when no more characters are left to read.</returns>
        public static int Read(this TextReader reader, char[] buffer, int index, int count, CancellationToken ct)
        {
            var task = reader.ReadAsync(buffer, index, count);
            try { task.Wait(ct); }
            catch (AggregateException ex) { throw ex.InnerException; }
            return task.Result;
        }

        /// <summary>
        /// Reads a specified maximum number of characters from the current text reader and writes the data to a buffer, beginning at the specified index.
        /// </summary>
        /// <param name="reader">Text reader</param>
        /// <param name="buffer">When this method returns, contains the specified character array with the values between index and (index + count - 1) replaced by the characters read from the current source</param>
        /// <param name="index">The position in buffer at which to begin writing</param>
        /// <param name="count">The maximum number of characters to read. If the end of the text is reached before the specified number of characters is read into the buffer, the current method returns.</param>
        /// <param name="ct">The token to monitor for cancellation requests. The default value is System.Threading.CancellationToken.None</param>
        /// <returns>The total number of bytes read into the buffer. The result value can be less than the number of bytes requested if the number of bytes currently available is less than the requested number, or it can be 0 (zero) if the end of the text has been reached.</returns>
        public static int ReadBlock(this TextReader reader, char[] buffer, int index, int count, CancellationToken ct)
        {
            var task = reader.ReadBlockAsync(buffer, index, count);
            try { task.Wait(ct); }
            catch (AggregateException ex) { throw ex.InnerException; }
            return task.Result;
        }

        /// <summary>
        /// Reads a line of characters asynchronously and returns the data as a string.
        /// </summary>
        /// <param name="reader">Text reader</param>
        /// <param name="ct">The token to monitor for cancellation requests. The default value is System.Threading.CancellationToken.None</param>
        /// <returns>The next line from the reader, or null if all characters have been read.</returns>
        public static string ReadLine(this TextReader reader, CancellationToken ct)
        {
            var task = reader.ReadLineAsync();
            try { task.Wait(ct); }
            catch (AggregateException ex) { throw ex.InnerException; }
            return task.Result;
        }

        /// <summary>
        /// Reads all characters from the current position to the end of the text reader and returns them as one string.
        /// </summary>
        /// <param name="reader">Text reader</param>
        /// <param name="ct">The token to monitor for cancellation requests. The default value is System.Threading.CancellationToken.None</param>
        /// <returns>A string that contains all characters from the current position to the end of the text reader</returns>
        public static string ReadToEnd(this TextReader reader, CancellationToken ct)
        {
            var task = reader.ReadToEndAsync();
            try { task.Wait(ct); }
            catch (AggregateException ex) { throw ex.InnerException; }
            return task.Result;
        }
    }
}

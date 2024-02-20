/*
    eduOAuth - OAuth 2.0 Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;

namespace eduOAuth
{
    /// <summary>
    /// HTTP listener (server)
    /// </summary>
    public class HttpListener : TcpListener
    {
        #region Fields

        /// <summary>
        /// Executing assembly
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();

        /// <summary>
        /// Filename extension - MIME type dictionary
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Dictionary<string, string> MimeTypes = new Dictionary<string, string>()
        {
            { ".css", "text/css" },
            { ".js" , "text/javascript" },
        };

        #endregion

        #region Constructors

        /// <inheritdoc/>
        public HttpListener(IPEndPoint localEP) :
            base(localEP)
        { }

        /// <inheritdoc/>
        public HttpListener(IPAddress localaddr, int port) :
            base(localaddr, port)
        { }

        #endregion

        #region Methods

        /// <summary>
        /// Starts listening and accepting clients in the background
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        public void Start(CancellationToken ct = default)
        {
            // Launch TCP listener.
            Start(10);
            var gettingReady = new CancellationTokenSource();
            new Thread(new ThreadStart(
                () =>
                {
                    gettingReady.Cancel();
                    for (; ; )
                    {
                        // Wait for the agent request and accept it.
                        TcpClient client = null;
                        try { client = AcceptTcpClient(); }
                        catch (InvalidOperationException) { break; }
                        catch (SocketException) { break; }
                        new Thread(ProcessRequest).Start(client);
                    }
                })).Start();
            CancellationTokenSource.CreateLinkedTokenSource(gettingReady.Token, ct).Token.WaitHandle.WaitOne();
        }

        /// <summary>
        /// Process a single HTTP request
        /// </summary>
        /// <param name="param">HTTP peer/client of type <see cref="TcpClient"/></param>
        private void ProcessRequest(object param)
        {
            try
            {
                // Receive agent request.
                var client = (TcpClient)param;
                using (var stream = client.GetStream())
                    try
                    {
                        // Read HTTP request header.
                        var headerStream = new MemoryStream(8192);
                        var terminator = new byte[4];
                        var modulus = terminator.Length;
                        for (var i = 0; ; i = (i + 1) % modulus)
                        {
                            var data = stream.ReadByte();
                            if (data == -1)
                                break;
                            headerStream.WriteByte((byte)data);
                            terminator[i] = (byte)data;
                            if (terminator[(i + modulus - 3) % modulus] == '\r' &&
                                terminator[(i + modulus - 2) % modulus] == '\n' &&
                                terminator[(i + modulus - 1) % modulus] == '\r' &&
                                terminator[(i + modulus - 0) % modulus] == '\n')
                                break;
                        }
                        headerStream.Seek(0, SeekOrigin.Begin);

                        string[] requestLine = null;
                        var requestHeaders = new NameValueCollection();
                        using (var reader = new StreamReader(headerStream, Encoding.UTF8, false))
                        {
                            // Parse start HTTP request line.
                            var line = reader.ReadLine();
                            if (string.IsNullOrEmpty(line))
                                throw new HttpException(400, string.Format(Resources.Strings.ErrorHttp400, line));
                            requestLine = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                            if (requestLine.Length < 3)
                                throw new HttpException(400, string.Format(Resources.Strings.ErrorHttp400, line));
                            switch (requestLine[0].ToUpperInvariant())
                            {
                                case "GET":
                                case "POST":
                                    break;
                                default:
                                    throw new HttpException(405, string.Format(Resources.Strings.ErrorHttp405, requestLine[0]));
                            }

                            // Parse request headers.
                            var headerSeparators = new char[] { ':' };
                            string fieldName = null;
                            for (; ; )
                            {
                                line = reader.ReadLine();
                                if (string.IsNullOrEmpty(line))
                                    break;
                                else if (fieldName == null || line[0] != ' ' && line[0] != '\t')
                                {
                                    var header = line.Split(headerSeparators, 2);
                                    if (header.Length < 2)
                                        throw new HttpException(400, string.Format(Resources.Strings.ErrorHttp400, line));
                                    fieldName = header[0].Trim();
                                    if (requestHeaders[fieldName] == null)
                                        requestHeaders.Add(fieldName, header[1].Trim());
                                    else
                                        requestHeaders[fieldName] += "," + header[1].Trim();
                                }
                                else
                                    requestHeaders[fieldName] += " " + line.Trim();
                            }
                        }

                        var contentLengthStr = requestHeaders["Content-Length"];
                        if (contentLengthStr != null && long.TryParse(contentLengthStr, out var contentLength))
                        {
                            // Read request content.
                            var buffer = new byte[client.ReceiveBufferSize];
                            while (contentLength > 0)
                            {
                                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                                if (bytesRead == 0)
                                    break;

                                contentLength -= bytesRead;
                            }
                        }

                        var uri = new Uri(string.Format("http://{0}:{1}{2}", IPAddress.Loopback, ((IPEndPoint)LocalEndpoint).Port, requestLine[1]));
                        if (uri.AbsolutePath.ToLowerInvariant() == "/callback")
                        {
                            OnHttpCallback(client, new HttpCallbackEventArgs(uri));

                            // Redirect agent to the finished page. This clears the explicit OAuth callback URI from agent location, and prevents page refreshes to reload /callback with stale data.
                            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                                writer.Write(string.Format("HTTP/1.0 301 Moved Permanently\r\nLocation: http://{0}:{1}/finished\r\n\r\n", IPAddress.Loopback, ((IPEndPoint)LocalEndpoint).Port));
                        }
                        else
                        {
                            var e = new HttpRequestEventArgs(uri);
                            OnHttpRequest(client, e);
                            using (e.Content)
                            {
                                // Send content.
                                var responseHeaders = Encoding.ASCII.GetBytes(string.Format("HTTP/1.0 200 OK\r\nContent-Type: {0}\r\nContent-Length: {1}\r\n\r\n",
                                    e.Type ?? "application/octet-stream",
                                    e.Content.Length));
                                stream.Write(responseHeaders, 0, responseHeaders.Length);
                                e.Content.CopyTo(stream);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Send response to the agent.
                        var statusCode = ex is HttpException httpEx ? httpEx.GetHttpCode() : 500;
                        using (var resourceStream = ExecutingAssembly.GetManifestResourceStream("eduOAuth.Resources.Html.error.html"))
                        using (var reader = new StreamReader(resourceStream, true))
                        {
                            string response;
                            try
                            {
                                var ci = Thread.CurrentThread.CurrentUICulture;
                                response = string.Format(reader.ReadToEnd(),
                                    ci.Name,
                                    ci.TextInfo.IsRightToLeft ? "rtl" : "ltr",
                                    HttpUtility.HtmlEncode(Resources.Strings.HtmlErrorTitle),
                                    HttpUtility.HtmlEncode(ex.Message),
                                    HttpUtility.HtmlEncode(Resources.Strings.HtmlErrorDescription),
                                    HttpUtility.HtmlEncode(Resources.Strings.HtmlErrorDetails),
                                    HttpUtility.HtmlEncode(ex.ToString()));
                            }
                            catch { response = HttpUtility.HtmlEncode(ex.ToString()); }
                            try
                            {
                                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                                    writer.Write(string.Format("HTTP/1.0 {0} Error\r\nContent-Type: text/html; charset=UTF-8\r\nContent-Length: {1}\r\n\r\n{2}", statusCode, response.Length, response));
                            }
                            catch { }
                        }
                    }
            }
            catch { }
        }

        /// <summary>
        /// Raises <see cref="HttpCallback"/> event
        /// </summary>
        /// <param name="sender">Event sender - a <see cref="TcpClient"/> object representing agent client</param>
        /// <param name="e">Event parameters</param>
        protected virtual void OnHttpCallback(object sender, HttpCallbackEventArgs e)
        {
            HttpCallback?.Invoke(sender, e);
        }

        /// <summary>
        /// Occurs when OAuth callback received.
        /// </summary>
        /// <remarks>Sender is the TCP client <see cref="TcpClient"/>.</remarks>
        public event EventHandler<HttpCallbackEventArgs> HttpCallback;

        /// <summary>
        /// Raises <see cref="HttpRequest"/> event
        /// </summary>
        /// <param name="sender">Event sender - a <see cref="TcpClient"/> object representing agent client</param>
        /// <param name="e">Event parameters</param>
        protected virtual void OnHttpRequest(object sender, HttpRequestEventArgs e)
        {
            HttpRequest?.Invoke(sender, e);
            if (e.Content != null)
                return;

            // The event handlers provided no data. Fall-back to default.
            switch (e.Uri.AbsolutePath.ToLowerInvariant())
            {
                case "/finished":
                    // Return response.
                    var ci = Thread.CurrentThread.CurrentUICulture;
                    using (var resourceStream = ExecutingAssembly.GetManifestResourceStream("eduOAuth.Resources.Html.finished.html"))
                    using (var reader = new StreamReader(resourceStream, true))
                    {
                        e.Type = "text/html; charset=UTF-8";
                        e.Content = new MemoryStream(Encoding.UTF8.GetBytes(
                            string.Format(reader.ReadToEnd(),
                                ci.Name,
                                ci.TextInfo.IsRightToLeft ? "rtl" : "ltr",
                                HttpUtility.HtmlEncode(Resources.Strings.HtmlFinishedTitle),
                                HttpUtility.HtmlEncode(Resources.Strings.HtmlFinishedDescription))));
                    }
                    return;

                case "/script.js":
                case "/style.css":
                    // Return static content.
                    e.Type = MimeTypes[Path.GetExtension(e.Uri.LocalPath)];
                    e.Content = ExecutingAssembly.GetManifestResourceStream("eduOAuth.Resources.Html" + e.Uri.AbsolutePath.Replace('/', '.'));
                    return;
            }

            throw new HttpException(404, string.Format(Resources.Strings.ErrorHttp404, e.Uri.AbsolutePath));
        }

        /// <summary>
        /// Occurs when browser requests data.
        /// </summary>
        /// <remarks>Sender is the TCP client <see cref="TcpClient"/>.</remarks>
        public event EventHandler<HttpRequestEventArgs> HttpRequest;

        #endregion
    }
}

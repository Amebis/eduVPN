/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Net;

namespace eduVPN.Views.Windows
{
    /// <summary>
    /// Connect wizard base class
    /// </summary>
    public class ConnectWizardBase : Window
    {
        #region Fields

        /// <summary>
        /// HTTP listener for OAuth authorization callback and response
        /// </summary>
        private eduOAuth.HttpListener _http_listener;

        #endregion

        #region Properties

        /// <summary>
        /// HTTP listener IP address and port number
        /// </summary>
        protected IPEndPoint HttpListenerEndpoint
        {
            get
            {
                return (IPEndPoint)_http_listener.LocalEndpoint;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a window
        /// </summary>
        public ConnectWizardBase()
        {
            // Launch HTTP listener on the loopback interface.
            _http_listener = new eduOAuth.HttpListener(IPAddress.Loopback, 0);
            _http_listener.HttpCallback += OnHttpCallback;
            _http_listener.Start();
        }

        /// <summary>
        /// This method is called when OAuth callback is called
        /// </summary>
        /// <param name="sender">HTTP peer/client of type <see cref="System.Net.Sockets.TcpClient"/></param>
        /// <param name="e">Event arguments</param>
        protected virtual void OnHttpCallback(object sender, eduOAuth.HttpCallbackEventArgs e)
        {
        }

        /// <inheritdoc/>
        protected override void OnClosed(EventArgs e)
        {
            // Stop the OAuth listener.
            _http_listener.Stop();

            base.OnClosed(e);
        }

        #endregion
    }
}

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2018 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Threading;

namespace eduVPN.Xml
{
    /// <summary>
    /// Unexpected parameter type.
    /// </summary>
    [Serializable]
    public class WebExceptionEx : WebException
    {
        #region Properties

        /// <summary>
        /// The response text
        /// </summary>
        public string ResponseText { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="ex">Original <see cref="WebException"/></param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        public WebExceptionEx(WebException ex, CancellationToken ct = default(CancellationToken)) :
            base(ex.Message, ex.InnerException, ex.Status, ex.Response)
        {
            if (ex.Response is HttpWebResponse response_http)
            {
                // Read the response from server and save it.
                using (var stream_reader = new StreamReader(response_http.GetResponseStream(), Encoding.GetEncoding(response_http.CharacterSet)))
                {
                    var task = stream_reader.ReadToEndAsync();
                    try { task.Wait(ct); }
                    catch (AggregateException ex2) { throw ex2.InnerException; }
                    ResponseText = task.Result;
                }
            }
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return String.IsNullOrEmpty(ResponseText) ?
                base.ToString() :
                base.ToString() + "\r\n-----BEGIN RESPONSE-----\r\n" + ResponseText + "\r\n-----END RESPONSE-----\r\n";
        }

        #endregion

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected WebExceptionEx(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ResponseText = (string)info.GetValue("ResponseText", typeof(string));
        }

        /// <inheritdoc/>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ResponseText", ResponseText);
        }

        #endregion
    }
}

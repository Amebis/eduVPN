/*
    eduOAuth - OAuth 2.0 Library for eduVPN (and beyond)

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;

namespace eduOAuth
{
    /// <summary>
    /// Bearer access token
    /// </summary>
    [Serializable]
    public class BearerToken : AccessToken
    {
        #region Constructors

        /// <summary>
        /// Bearer access token (RFC 6750)
        /// </summary>
        /// <param name="obj">An object representing access token as returned by the authentication server</param>
        /// <param name="authorized">Timestamp of the initial authorization</param>
        /// <remarks>
        /// <a href="https://tools.ietf.org/html/rfc6750">RFC6750</a>
        /// </remarks>
        public BearerToken(Dictionary<string, object> obj, DateTimeOffset authorized) :
            base(obj, authorized)
        {
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override void AddToRequest(WebRequest request)
        {
            request.Headers.Add(string.Format("Authorization: Bearer {0}", new NetworkCredential("", Token).Password));
        }

        #endregion

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected BearerToken(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}

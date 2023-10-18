/*
    eduOAuth - OAuth 2.0 Library for eduVPN (and beyond)

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace eduOAuth
{
    /// <summary>
    /// Invalid access token
    /// </summary>
    [Serializable]
    public class InvalidToken : AccessToken
    {
        #region Constructors

        /// <summary>
        /// Creates an invalid access token
        /// </summary>
        public InvalidToken() :
            base(new Dictionary<string, object>()
            {
                { "access_token", "" },
                { "expires_in", 10 },
            },
            DateTimeOffset.MinValue)
        {
        }

        #endregion

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected InvalidToken(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}

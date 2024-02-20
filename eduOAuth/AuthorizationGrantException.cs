/*
    eduOAuth - OAuth 2.0 Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace eduOAuth
{
    /// <summary>
    /// OAuth authorization server returned an error.
    /// </summary>
    /// <remarks>
    /// <a href="https://tools.ietf.org/html/rfc6749#section-4.1.2.1">RFC6749 Section 4.1.2.1</a>
    /// </remarks>
    [Serializable]
    public class AuthorizationGrantException : Exception
    {
        #region Data Types

        /// <summary>
        /// An error type
        /// </summary>
        public enum ErrorCodeType
        {
            /// <summary>
            /// Unknown error.
            /// </summary>
            Unknown,

            /// <summary>
            /// The request is missing a required parameter, includes an
            /// invalid parameter value, includes a parameter more than
            /// once, or is otherwise malformed.
            /// </summary>
            InvalidRequest,

            /// <summary>
            /// The client is not authorized to request an authorization
            /// code using this method.
            /// </summary>
            UnauthorizedClient,

            /// <summary>
            /// The resource owner or authorization server denied the
            /// request.
            /// </summary>
            AccessDenied,

            /// <summary>
            /// The authorization server does not support obtaining an
            /// authorization code using this method.
            /// </summary>
            UnsupportedResponseType,

            /// <summary>
            /// The requested scope is invalid, unknown, or malformed.
            /// </summary>
            InvalidScope,

            /// <summary>
            /// The authorization server encountered an unexpected
            /// condition that prevented it from fulfilling the request.
            /// (This error code is needed because a 500 Internal Server
            /// Error HTTP status code cannot be returned to the client
            /// via an HTTP redirect.)
            /// </summary>
            ServerError,

            /// <summary>
            /// The authorization server is currently unable to handle
            /// the request due to a temporary overloading or maintenance
            /// of the server.  (This error code is needed because a 503
            /// Service Unavailable HTTP status code cannot be returned
            /// to the client via an HTTP redirect.)
            /// </summary>
            TemporarilyUnavailable,
        }

        #endregion

        #region Properties

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                string msg;
                switch (ErrorCode)
                {
                    case ErrorCodeType.InvalidRequest:
                        msg = Resources.Strings.ErrorAuthorizationGrantInvalidRequest;
                        break;

                    case ErrorCodeType.UnauthorizedClient:
                        msg = Resources.Strings.ErrorAuthorizationGrantUnauthorizedClient;
                        break;

                    case ErrorCodeType.AccessDenied:
                        msg = Resources.Strings.ErrorAuthorizationGrantAccessDenied;
                        break;

                    case ErrorCodeType.UnsupportedResponseType:
                        msg = Resources.Strings.ErrorAuthorizationGrantUnsupportedResponseType;
                        break;

                    case ErrorCodeType.InvalidScope:
                        msg = Resources.Strings.ErrorAuthorizationGrantInvalidScope;
                        break;

                    case ErrorCodeType.ServerError:
                        msg = Resources.Strings.ErrorAuthorizationGrantServerError;
                        break;

                    case ErrorCodeType.TemporarilyUnavailable:
                        msg = Resources.Strings.ErrorAuthorizationGrantTemporarilyUnavailable;
                        break;

                    default:
                        msg = null;
                        break;
                }

                if (base.Message != null)
                    msg = msg != null ? string.Format("{0}\n{1}", msg, base.Message) : base.Message;

                if (ErrorUri != null)
                    msg = msg != null ? string.Format("{0}\n{1}", msg, ErrorUri.ToString()) : ErrorUri.ToString();

                return msg;
            }
        }

        /// <summary>
        /// Error code
        /// </summary>
        public ErrorCodeType ErrorCode { get; }

        /// <summary>
        /// Error URI
        /// </summary>
        public Uri ErrorUri { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an exception
        /// </summary>
        /// <param name="error">An RFC6749 error identifier</param>
        /// <param name="errorDescription">Human-readable text providing additional information</param>
        /// <param name="errorUri">A URI identifying a human-readable web page with information about the error</param>
        public AuthorizationGrantException(string error, string errorDescription, string errorUri) :
            base(errorDescription)
        {
            switch (error.ToLowerInvariant())
            {
                case "invalid_request":
                    ErrorCode = ErrorCodeType.InvalidRequest;
                    break;

                case "unauthorized_client":
                    ErrorCode = ErrorCodeType.UnauthorizedClient;
                    break;

                case "access_denied":
                    ErrorCode = ErrorCodeType.AccessDenied;
                    break;

                case "unsupported_response_type":
                    ErrorCode = ErrorCodeType.UnsupportedResponseType;
                    break;

                case "invalid_scope":
                    ErrorCode = ErrorCodeType.InvalidScope;
                    break;

                case "server_error":
                    ErrorCode = ErrorCodeType.ServerError;
                    break;

                case "temporarily_unavailable":
                    ErrorCode = ErrorCodeType.TemporarilyUnavailable;
                    break;

                default:
                    ErrorCode = ErrorCodeType.Unknown;
                    break;
            }

            if (errorUri != null)
                ErrorUri = new Uri(errorUri);
        }

        #endregion

        #region ISerializable Support

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> populated with data.</param>
        /// <param name="context">The source of this deserialization.</param>
        protected AuthorizationGrantException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ErrorCode = (ErrorCodeType)info.GetValue("ErrorCode", typeof(ErrorCodeType));
            ErrorUri = (Uri)info.GetValue("ErrorUri", typeof(Uri));
        }

        /// <inheritdoc/>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ErrorCode", ErrorCode);
            info.AddValue("ErrorUri", ErrorUri);
        }

        #endregion
    }
}

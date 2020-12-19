﻿/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN certificate check result
    /// </summary>
    public class CertificateCheck : JSON.ILoadableItem
    {
        #region Data Types

        /// <summary>
        /// An error type
        /// </summary>
        public enum ReasonType
        {
            /// <summary>
            /// Unknown result.
            /// </summary>
            Unknown,

            /// <summary>
            /// Certificate is valid and will (probably) be accepted by the server.
            /// </summary>
            Valid,

            /// <summary>
            /// Certificate is not valid for an unknown reason.
            /// </summary>
            Invalid,

            /// <summary>
            /// CN never exist, was deleted by the user, or the server was reinstalled and the certificate is no longer there.
            /// </summary>
            CertificateMissing,

            /// <summary>
            /// The user account was disabled by an administrator.
            /// </summary>
            UserDisabled,

            /// <summary>
            /// The certificate was disabled by an administrator.
            /// </summary>
            CertificateDisabled,

            /// <summary>
            /// The certificate is not yet valid.
            /// </summary>
            CertificateNotYetValid,

            /// <summary>
            /// The certificate is no longer valid (expired).
            /// </summary>
            CertificateExpired,
        }

        #endregion

        #region Properties

        /// <summary>
        /// Certificate check result
        /// </summary>
        public ReasonType Result { get; private set; }

        #endregion

        #region ILoadableItem Support

        /// <summary>
        /// Loads client certificate check result from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>is_valid</c> element with boolean value.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public void Load(object obj)
        {
            if (!(obj is Dictionary<string, object> obj2))
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());

            // Set check result.
            if (eduJSON.Parser.GetValue<bool>(obj2, "is_valid"))
                Result = ReasonType.Valid;
            else if (eduJSON.Parser.GetValue(obj2, "reason", out string reason))
            {
                // Parse reason for check failure.
                switch (reason)
                {
                    case "certificate_missing": Result = ReasonType.CertificateMissing; break;
                    case "user_disabled": Result = ReasonType.UserDisabled; break;
                    case "certificate_disabled": Result = ReasonType.CertificateDisabled; break;
                    case "certificate_not_yet_valid": Result = ReasonType.CertificateNotYetValid; break;
                    case "certificate_expired": Result = ReasonType.CertificateExpired; break;
                    default: Result = ReasonType.Invalid; break;
                }
            }
            else
            {
                // No reason specified.
                Result = ReasonType.Invalid;
            }
        }

        #endregion
    }
}

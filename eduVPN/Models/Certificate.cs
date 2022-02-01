/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Security;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN certificate with a key
    /// </summary>
    public class Certificate : JSON.ILoadableItem
    {
        #region Properties

        /// <summary>
        /// PEM encoded X509 Certificate
        /// </summary>
        public string Cert { get; private set; }

        /// <summary>
        /// PEM encoded X509 private key
        /// </summary>
        public SecureString PrivateKey { get; private set; }

        #endregion

        #region ILoadableItem Support

        /// <summary>
        /// Loads client certificate from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>certificate</c> and <c>private_key</c> elements. All elements should be PEM encoded strings.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public void Load(object obj)
        {
            if (!(obj is Dictionary<string, object> obj2))
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());

            Cert = eduJSON.Parser.GetValue<string>(obj2, "certificate");
            PrivateKey = new NetworkCredential("", eduJSON.Parser.GetValue<string>(obj2, "private_key")).SecurePassword;
        }

        /// <summary>
        /// Decodes PEM encoded BLOB
        /// </summary>
        /// <param name="pem">PEM encoded text</param>
        /// <param name="section">Section name to decode ("CERTIFICATE", "PRIVATE KEY", etc.)</param>
        /// <returns>BLOB</returns>
        /// <remarks>
        /// Based on this <a href="https://stackoverflow.com/a/10498045/2071884">example</a>.
        /// </remarks>
        public static byte[] GetBytesFromPEM(string pem, string section)
        {
            var header = string.Format("-----BEGIN {0}-----", section);
            var footer = string.Format("-----END {0}-----", section);

            var start = pem.IndexOf(header, StringComparison.Ordinal);
            if (start < 0)
                return null;
            start += header.Length;

            var end = pem.IndexOf(footer, start, StringComparison.Ordinal);
            if (end < 0)
                return null;

            return Convert.FromBase64String(pem.Substring(start, end - start));
        }

        #endregion
    }
}

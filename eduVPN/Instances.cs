/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;

namespace eduVPN
{
    /// <summary>
    /// An eduVPN list of instances = VPN service providers
    /// </summary>
    public class Instances : List<Instance>
    {
        #region Data Types

        /// <summary>
        /// Authorization type
        /// </summary>
        public enum AuthorizationType
        {
            /// <summary>
            /// Access token is specific to each instance and cannot be used by other instances.
            /// </summary>
            Local,

            /// <summary>
            /// Access token is issued by a central OAuth server; all instances accept this token.
            /// </summary>
            Federated,

            /// <summary>
            /// Access token from any instance can be used by any other instance.
            /// </summary>
            Distributed
        }

        #endregion

        #region Properties

        /// <summary>
        /// Authorization type
        /// </summary>
        public AuthorizationType AuthType { get; }

        /// <summary>
        /// Version sequence
        /// </summary>
        public uint Sequence { get; }

        /// <summary>
        /// Signature timestamp
        /// </summary>
        public DateTime? SignedAt { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Loads instance list from the given URI
        /// </summary>
        /// <param name="uri">Typically <c>&quot;https://static.eduvpn.nl/instances.json&quot;</c></param>
        /// <param name="pub_key">Public key for signature verification; or <c>null</c> if signature verification is not required.</param>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public Instances(Uri uri, byte[] pub_key = null)
        {
            // Load instances data.
            var data = new byte[1048576]; // Limit to 1MiB
            int data_size;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.CachePolicy = noCachePolicy;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            {
                // Spawn data read in the background, to allow loading signature in parallel.
                var read = stream.BeginRead(data, 0, data.Length, null, null);

                if (pub_key != null)
                {
                    // Generate signature URI.
                    var builder_sig = new UriBuilder(uri);
                    builder_sig.Path += ".sig";

                    // Load signature.
                    byte[] signature;
                    request = (HttpWebRequest)WebRequest.Create(builder_sig.Uri);
                    request.CachePolicy = noCachePolicy;
                    using (HttpWebResponse signature_response = (HttpWebResponse)request.GetResponse())
                    using (Stream signature_stream = signature_response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(signature_stream))
                        signature = Convert.FromBase64String(reader.ReadToEnd());

                    // Wait for the data to arrive.
                    data_size = stream.EndRead(read);

                    // Verify signature.
                    using (eduEd25519.ED25519 key = new eduEd25519.ED25519(pub_key))
                        if (!key.VerifyDetached(data, 0, data_size, signature))
                            throw new System.Security.SecurityException(String.Format(Resources.ErrorInvalidSignature, uri));
                } else {
                    // Wait for the data to arrive.
                    data_size = stream.EndRead(read);
                }
            }

            // Parse data.
            var obj = (Dictionary<string, object>)eduJSON.Parser.Parse(Encoding.UTF8.GetString(data, 0, data_size));

            // Parse all instances listed.
            foreach (var el in eduJSON.Parser.GetValue< List<object> >(obj, "instances"))
                if (el.GetType() == typeof(Dictionary<string, object>))
                    Add(new Instance((Dictionary<string, object>)el));

            // Parse sequence.
            Sequence = eduJSON.Parser.GetValue(obj, "seq", out int seq) ? (uint)seq : 0;

            // Parse authorization data.
            if (eduJSON.Parser.GetValue(obj, "authorization_type", out string authorization_type))
            {
                switch (authorization_type.ToLower())
                {
                    case "federated": AuthType = AuthorizationType.Federated; break;
                    case "distributed": AuthType = AuthorizationType.Distributed; break;
                    default: AuthType = AuthorizationType.Local; break; // Assume local authorization type on all other values.
                }
            }
            else
                AuthType = AuthorizationType.Local;

            SignedAt = null;
            if (pub_key != null)
            {
                // Parse signed date.
                if (eduJSON.Parser.GetValue(obj, "signed_at", out string signed_at) && DateTime.TryParse(signed_at, out DateTime result))
                    SignedAt = result;
            }
        }

        #endregion
    }
}

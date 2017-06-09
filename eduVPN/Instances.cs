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
        /// <summary>
        /// Loads instance list from the given URI
        /// </summary>
        /// <param name="uri">Typically <c>&quot;https://static.eduvpn.nl/instances.json&quot;</c></param>
        /// <param name="pub_key">Public key for signature verification; or <c>null</c> if signature verification is not required.</param>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public Instances(Uri uri, byte[] pub_key = null)
        {
            // Load instances data.
            byte[] data;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.CachePolicy = noCachePolicy;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (BinaryReader reader = new BinaryReader(stream))
                data = reader.ReadBytes(1048576); // Limit to 1MiB

            if (pub_key != null)
            {
                // Generate signature URI.
                var builder_sig = new UriBuilder(uri);
                builder_sig.Path += ".sig";

                // Load signature.
                byte[] signature;
                request = (HttpWebRequest)WebRequest.Create(builder_sig.Uri);
                request.CachePolicy = noCachePolicy;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                    signature = Convert.FromBase64String(reader.ReadToEnd());

                // Verify signature.
                using (eduEd25519.ED25519 key = new eduEd25519.ED25519(pub_key))
                {
                    if (!key.VerifyDetached(data, signature))
                        throw new System.Security.SecurityException(String.Format(Resources.ErrorInvalidSignature, uri));
                }
            }

            // Parse data.
            var obj = (Dictionary<string, object>)eduJSON.Parser.Parse(Encoding.UTF8.GetString(data));

            // Parse all instances listed.
            object instances;
            if (obj.TryGetValue("instances", out instances) && instances.GetType() == typeof(List<object>))
            {
                foreach (var el in (List<object>)instances)
                    if (el.GetType() == typeof(Dictionary<string, object>))
                        Add(new Instance((Dictionary<string, object>)el));
            }

            // Parse sequence.
            object seq;
            Sequence = obj.TryGetValue("seq", out seq) && seq.GetType() == typeof(int) ? (uint)(int)seq : 0;

            // Parse authorization data.
            object authorization_type;
            if (obj.TryGetValue("authorization_type", out authorization_type) && authorization_type.GetType() == typeof(string))
            {
                switch (((string)authorization_type).ToLower())
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
                object signed_at;
                if (obj.TryGetValue("signed_at", out signed_at) && signed_at.GetType() == typeof(string))
                {
                    DateTime result;
                    if (DateTime.TryParse((string)signed_at, out result))
                        SignedAt = result;
                }
            }
        }

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
    }
}

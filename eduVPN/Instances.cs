/*
    Copyright 2017 Amebis

    This file is part of eduVPN.

    eduVPN is free software: you can redistribute it and/or modify it
    under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    eduVPN is distributed in the hope that it will be useful, but
    WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with eduVPN. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace eduVPN
{
    /// <summary>
    /// An eduVPN list of instances = VPN service providers
    /// </summary>
    public class Instances : List<Instance>
    {
        public Instances(Uri uri, byte[] pub_key = null)
        {
            // Load instances data.
            var client = new WebClient();
            var data = client.DownloadData(uri);

            if (pub_key != null)
            {
                // Generate signature URI.
                var builder_sig = new UriBuilder(uri);
                builder_sig.Path += ".sig";

                // Load signature.
                byte[] signature = Convert.FromBase64String(client.DownloadString(builder_sig.Uri));

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

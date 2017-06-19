/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace eduVPN
{
    /// <summary>
    /// An eduVPN list of instances = VPN service providers
    /// </summary>
    public class InstanceList : ObservableCollection<Instance>
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

        #region Fields

        private AuthorizationType _auth_type;
        private uint _sequence;
        private DateTime? _signed_at;

        #endregion

        #region Properties

        /// <summary>
        /// Authorization type
        /// </summary>
        public AuthorizationType AuthType { get => _auth_type; }

        /// <summary>
        /// Version sequence
        /// </summary>
        public uint Sequence { get => _sequence; }

        /// <summary>
        /// Signature timestamp
        /// </summary>
        public DateTime? SignedAt { get => _signed_at; }

        #endregion

        #region Methods

        /// <summary>
        /// Loads instance list from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>authorization_endpoint</c>, <c>token_endpoint</c> and other optional elements. All elements should be strings representing URI(s).</param>
        public void Load(Dictionary<string, object> obj)
        {
            Clear();

            // Parse all instances listed. Don't do it in parallel to preserve the sort order.
            foreach (var el in eduJSON.Parser.GetValue<List<object>>(obj, "instances"))
            {
                if (el.GetType() == typeof(Dictionary<string, object>))
                {
                    var instance = new Instance();
                    instance.Load((Dictionary<string, object>)el);
                    Add(instance);
                }
            }

            // Parse sequence.
            _sequence = eduJSON.Parser.GetValue(obj, "seq", out int seq) ? (uint)seq : 0;
            OnPropertyChanged(new PropertyChangedEventArgs("Sequence"));

            // Parse authorization data.
            if (eduJSON.Parser.GetValue(obj, "authorization_type", out string authorization_type))
            {
                switch (authorization_type.ToLower())
                {
                    case "federated": _auth_type = AuthorizationType.Federated; break;
                    case "distributed": _auth_type = AuthorizationType.Distributed; break;
                    default: _auth_type = AuthorizationType.Local; break; // Assume local authorization type on all other values.
                }
            }
            else
                _auth_type = AuthorizationType.Local;
            OnPropertyChanged(new PropertyChangedEventArgs("AuthType"));

            // Parse signed date.
            _signed_at = eduJSON.Parser.GetValue(obj, "signed_at", out string signed_at) && DateTime.TryParse(signed_at, out DateTime signed_at_date) ? signed_at_date : (DateTime?)null;
            OnPropertyChanged(new PropertyChangedEventArgs("SignedAt"));
        }

        /// <summary>
        /// Gets instance list from the given URI.
        /// </summary>
        /// <param name="uri">Typically <c>&quot;https://static.eduvpn.nl/instances.json&quot;</c></param>
        /// <param name="pub_key">Public key for signature verification; or <c>null</c> if signature verification is not required.</param>
        /// <param name="ct">The token to monitor for cancellation requests.</param>
        public static Dictionary<string, object> Get(Uri uri, byte[] pub_key = null, CancellationToken ct = default(CancellationToken))
        {
            var task = GetAsync(uri, pub_key, ct);
            try
            {
                task.Wait(ct);
                return task.Result;
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Gets instance list from the given URI asynchronously.
        /// </summary>
        /// <param name="uri">Typically <c>&quot;https://static.eduvpn.nl/instances.json&quot;</c></param>
        /// <param name="pub_key">Public key for signature verification; or <c>null</c> if signature verification is not required.</param>
        /// <param name="ct">The token to monitor for cancellation requests.</param>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static async Task<Dictionary<string, object>> GetAsync(Uri uri, byte[] pub_key = null, CancellationToken ct = default(CancellationToken))
        {
            // Spawn data loading.
            var data = new byte[1048576]; // Limit to 1MiB
            int data_size;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.CachePolicy = noCachePolicy;
            var response_task = request.GetResponseAsync();

            byte[] signature = null;
            Task<WebResponse> response_sig_task = null;
            if (pub_key != null)
            {
                // Generate signature URI.
                var builder_sig = new UriBuilder(uri);
                builder_sig.Path += ".sig";

                // Spawn signature loading.
                request = (HttpWebRequest)WebRequest.Create(builder_sig.Uri);
                request.CachePolicy = noCachePolicy;
                response_sig_task = request.GetResponseAsync();
            }

            // Wait for data to start comming in.
            using (var response = (HttpWebResponse)(await response_task))
            using (var stream = response.GetResponseStream())
            {
                // Spawn data read.
                var read_task = stream.ReadAsync(data, 0, data.Length, ct);

                if (pub_key != null)
                {
                    // Read the signature.
                    using (var response_sig = (HttpWebResponse)(await response_sig_task))
                    using (var stream_sig = response_sig.GetResponseStream())
                    using (var reader_sig = new StreamReader(stream_sig))
                        signature = Convert.FromBase64String(await reader_sig.ReadToEndAsync());
                }

                // Wait for the data to arrive.
                data_size = await read_task;
                if (read_task.IsCanceled)
                    throw new OperationCanceledException(ct);
            }

            if (pub_key != null)
            {
                ct.ThrowIfCancellationRequested();

                // Verify signature.
                using (eduEd25519.ED25519 key = new eduEd25519.ED25519(pub_key))
                    if (!key.VerifyDetached(data, 0, data_size, signature))
                        throw new System.Security.SecurityException(String.Format(Resources.ErrorInvalidSignature, uri));
            }

            // Parse data.
            return (Dictionary<string, object>)eduJSON.Parser.Parse(Encoding.UTF8.GetString(data, 0, data_size), ct);
        }

        #endregion
    }
}

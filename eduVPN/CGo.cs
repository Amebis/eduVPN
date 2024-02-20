/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduEx.System;
using eduVPN.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace eduVPN
{
    public class CGo
    {
        #region Data types

        /// <summary>
        /// Converts UTF-8 CGo char* to managed string
        /// </summary>
        class CGoToManagedStringMarshaller : ICustomMarshaler
        {
            #region Methods

            /// <summary>
            /// The one and only instance of marshaller.
            /// This marshaller is stateless, thus it does not require a separate instance for each conversion.
            /// </summary>
            static CGoToManagedStringMarshaller Default;

            public static ICustomMarshaler GetInstance(string cookie)
            {
                if (Default != null)
                    return Default;
                return Default = new CGoToManagedStringMarshaller();
            }

            #endregion

            #region ICustomMarshaler Support

            /// <summary>
            /// Converts the unmanaged data to managed data.
            /// </summary>
            /// <param name="pNativeData">A pointer to the unmanaged data to be wrapped.</param>
            /// <returns>An object that represents the managed view of the COM data.</returns>
            public object MarshalNativeToManaged(IntPtr pNativeData)
            {
                if (pNativeData == IntPtr.Zero)
                    return null;
                using (var data = new MemoryStream(1024))
                {
                    using (var w = new BinaryWriter(data))
                    {
                        byte chr;
                        for (int offset = 0; (chr = Marshal.ReadByte(pNativeData + offset)) != 0; ++offset)
                            w.Write(chr);
                    }
                    return Encoding.UTF8.GetString(data.ToArray());
                }
            }

            /// <summary>
            /// Converts the managed data to unmanaged data.
            /// </summary>
            /// <param name="ManagedObj">The managed object to be converted.</param>
            /// <returns>A pointer to the COM view of the managed object.</returns>
            /// <exception cref="NotImplementedException">Not implemented</exception>
            public IntPtr MarshalManagedToNative(object ManagedObj)
            {
                throw new NotImplementedException();
            }

            [DllImport("eduvpn_windows.dll", CallingConvention = CallingConvention.Cdecl)]
            private static extern void free_string(IntPtr addr);

            /// <summary>
            /// Performs necessary cleanup of the unmanaged data when it is no longer needed.
            /// </summary>
            /// <param name="pNativeData">A pointer to the unmanaged data to be destroyed.</param>
            public void CleanUpNativeData(IntPtr pNativeData)
            {
                if (pNativeData != IntPtr.Zero)
                    free_string(pNativeData);
            }

            /// <summary>
            /// Performs necessary cleanup of the managed data when it is no longer needed.
            /// </summary>
            /// <param name="ManagedObj">The managed object to be destroyed.</param>
            /// <exception cref="NotImplementedException">Not implemented</exception>
            public void CleanUpManagedData(object ManagedObj)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Returns the size of the native data to be marshaled.
            /// </summary>
            /// <returns>The size, in bytes, of the native data.</returns>
            /// <exception cref="NotImplementedException">Not implemented</exception>
            public int GetNativeDataSize()
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        /// <summary>
        /// Cancellable operation context
        /// </summary>
        class CGoContext : IDisposable
        {
            #region Fields

            readonly CancellationTokenRegistration CancellationTokenRegistration;

            #endregion

            #region Properties

            /// <summary>
            /// eduvpn-windows context handle
            /// </summary>
            public IntPtr Handle { get; }

            #endregion

            #region Constructors

            public CGoContext(CancellationToken ct = default)
            {
                Handle = make_context();
                CancellationTokenRegistration = ct.Register(() => cancel_context(Handle));
            }

            #endregion

            #region Methods

            [DllImport("eduvpn_windows.dll", CallingConvention = CallingConvention.Cdecl)]
            static extern IntPtr make_context();

            [DllImport("eduvpn_windows.dll", CallingConvention = CallingConvention.Cdecl)]
            static extern void free_context(IntPtr ctx);

            [DllImport("eduvpn_windows.dll", CallingConvention = CallingConvention.Cdecl)]
            static extern void cancel_context(IntPtr ctx);

            #endregion

            #region IDisposable Support
            /// <summary>
            /// Flag to detect redundant <see cref="Dispose(bool)"/> calls.
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private bool disposedValue = false;

            /// <summary>
            /// Called to dispose the object.
            /// </summary>
            /// <param name="disposing">Dispose managed objects</param>
            /// <remarks>
            /// To release resources for inherited classes, override this method.
            /// Call <c>base.Dispose(disposing)</c> within it to release parent class resources, and release child class resources if <paramref name="disposing"/> parameter is <c>true</c>.
            /// This method can get called multiple times for the same object instance. When the child specific resources should be released only once, introduce a flag to detect redundant calls.
            /// </remarks>
            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        CancellationTokenRegistration.Dispose();
                        free_context(Handle);
                    }
                    disposedValue = true;
                }
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
            /// </summary>
            /// <remarks>
            /// This method calls <see cref="Dispose(bool)"/> with <c>disposing</c> parameter set to <c>true</c>.
            /// To implement resource releasing override the <see cref="Dispose(bool)"/> method.
            /// </remarks>
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
            }
            #endregion
        }

        /// <summary>
        /// Available self-update description
        /// </summary>
        public class SelfUpdatePackage
        {
            #region Fields

            Uri BaseUri;

            #endregion

            #region Properties

            /// <summary>
            /// Update file command line arguments
            /// </summary>
            public string Arguments { get; private set; }

            /// <summary>
            /// List of update file download URIs
            /// </summary>
            public List<Uri> Uris { get; private set; }

            /// <summary>
            /// Available product version
            /// </summary>
            public Version Version { get; private set; }

            /// <summary>
            /// Product changelog URI
            /// </summary>
            public Uri Changelog { get; private set; }

            /// <summary>
            /// Update file SHA-256 hash
            /// </summary>
            public byte[] Hash { get; private set; }

            #endregion

            #region Constructors

            public SelfUpdatePackage(Uri baseUri)
            {
                BaseUri = baseUri;
            }

            /// <summary>
            /// Constructs self-update description
            /// </summary>
            public SelfUpdatePackage(Uri baseUri, IReadOnlyDictionary<string, object> obj) : this(baseUri)
            {
                Arguments = eduJSON.Parser.GetValue(obj, "arguments", out string arguments) ? arguments : null;
                Uris = new List<Uri>(((List<object>)obj["uri"]).Select(u => u is string uStr ? new Uri(BaseUri, uStr) : null));
                Version = new Version((string)obj["version"]);
                Changelog = eduJSON.Parser.GetValue(obj, "changelog_uri", out string changelogUri) && changelogUri != null ? new Uri(BaseUri, changelogUri) : null;
                Hash = ((string)obj["hash-sha256"]).FromHexToBin();
            }

            #endregion
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SetProgress([MarshalAs(UnmanagedType.R4)] float value);

        /// <summary>
        /// Terminal Services session monitor
        /// </summary>
        public class SessionMonitor : IDisposable
        {
            #region Data types

            /// <summary>
            /// Event type
            /// </summary>
            public enum Event : uint
            {
                ConsoleConnect       = 0x1 /*WTS_CONSOLE_CONNECT*/,
                ConsoleDisconnect    = 0x2 /*WTS_CONSOLE_DISCONNECT*/,
                RemoteConnect        = 0x3 /*WTS_REMOTE_CONNECT*/,
                RemoteDisconnect     = 0x4 /*WTS_REMOTE_DISCONNECT*/,
                SessionReportLogon   = 0x5 /*WTS_SESSION_LOGON */| 0x8000000,
                SessionLogon         = 0x5 /*WTS_SESSION_LOGON*/,
                SessionLogoff        = 0x6 /*WTS_SESSION_LOGOFF*/,
                SessionLock          = 0x7 /*WTS_SESSION_LOCK*/,
                SessionUnlock        = 0x8 /*WTS_SESSION_UNLOCK*/,
                SessionRemoteControl = 0x9 /*WTS_SESSION_REMOTE_CONTROL*/,
                SessionCreate        = 0xa /*WTS_SESSION_CREATE*/,
                SessionTerminate     = 0xb /*WTS_SESSION_TERMINATE*/,
            }

            #endregion

            #region Properties

            /// <summary>
            /// eduvpn-windows session monitor handle
            /// </summary>
            IntPtr Handle { get; }

            #endregion

            #region Constructors

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void OnWTSChange(Event evnt, uint sessionId);

            readonly OnWTSChange Callback;

            [DllImport("eduvpn_windows.dll", CallingConvention = CallingConvention.Cdecl)]
            static extern CGoPtrPtr start_multisession_monitoring(OnWTSChange onWTSChange);

            /// <summary>
            /// Create new session monitor
            /// </summary>
            /// <param name="onWTSChange">Called on any session change</param>
            public SessionMonitor(Action<Event, uint> onWTSChange)
            {
                Callback = new OnWTSChange((evnt, sessionId) => onWTSChange(evnt, sessionId));
                var m = CGoToManagedStringMarshaller.GetInstance(null);
                var r = start_multisession_monitoring(Callback);
                try
                {
                    if (r.r1 != IntPtr.Zero)
                        throw new Exception((string)m.MarshalNativeToManaged(r.r1));
                    Handle = r.r0;
                }
                finally
                {
                    m.CleanUpNativeData(r.r1);
                }
            }

            #endregion

            #region IDisposable Support
            /// <summary>
            /// Flag to detect redundant <see cref="Dispose(bool)"/> calls.
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            bool disposedValue = false;

            [DllImport("eduvpn_windows.dll", CallingConvention = CallingConvention.Cdecl)]
            static extern void stop_multisession_monitoring(IntPtr handle);

            /// <summary>
            /// Called to dispose the object.
            /// </summary>
            /// <param name="disposing">Dispose managed objects</param>
            /// <remarks>
            /// To release resources for inherited classes, override this method.
            /// Call <c>base.Dispose(disposing)</c> within it to release parent class resources, and release child class resources if <paramref name="disposing"/> parameter is <c>true</c>.
            /// This method can get called multiple times for the same object instance. When the child specific resources should be released only once, introduce a flag to detect redundant calls.
            /// </remarks>
            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                        stop_multisession_monitoring(Handle);
                    disposedValue = true;
                }
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
            /// </summary>
            /// <remarks>
            /// This method calls <see cref="Dispose(bool)"/> with <c>disposing</c> parameter set to <c>true</c>.
            /// To implement resource releasing override the <see cref="Dispose(bool)"/> method.
            /// </remarks>
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
            }
            #endregion
        }

        #endregion

        #region Fields

        /// <summary>
        /// Used to convert Unix timestamps into <see cref="DateTimeOffset"/>
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        static readonly DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan(0, 0, 0));

        #endregion

        #region Methods

        [DllImport("eduvpn_windows.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern CGoPtrPtrPtr check_selfupdate(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string url,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string allowedSigners,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string productId,
            IntPtr ctx);

        public static Tuple<SelfUpdatePackage, Version> CheckSelfUpdate(ResourceRef discovery, string productId, CancellationToken ct = default)
        {
            using (var ctx = new CGoContext(ct))
            {
                var m = CGoToManagedStringMarshaller.GetInstance(null);
                var r = check_selfupdate(
                    discovery.Uri.AbsoluteUri,
                    string.Join(
                        "\0",
                        discovery.PublicKeys.Select(
                            k => string.Format(
                                "{0}|{1}",
                                Convert.ToBase64String(k.Data),
                                (int)k.SupportedAlgorithms)).ToArray()) + "\0",
                    productId,
                    ctx.Handle);
                try
                {
                    if (r.r2 != IntPtr.Zero)
                        throw new Exception((string)m.MarshalNativeToManaged(r.r2));
                    return Tuple.Create(
                        eduJSON.Parser.Parse((string)m.MarshalNativeToManaged(r.r0), ct) is IReadOnlyDictionary<string, object> p ? new SelfUpdatePackage(discovery.Uri, p) : null,
                        eduJSON.Parser.Parse((string)m.MarshalNativeToManaged(r.r1), ct) is string v ? new Version(v) : null);
                }
                finally
                {
                    m.CleanUpNativeData(r.r0);
                    m.CleanUpNativeData(r.r1);
                    m.CleanUpNativeData(r.r2);
                }
            }
        }

        [DllImport("eduvpn_windows.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string download_and_install_selfupdate(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string urls,
            byte[] hash,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string installerArguments,
            IntPtr ctx,
            SetProgress setProgress);

        public static void DownloadAndInstallSelfUpdate(
            IEnumerable<Uri> uris,
            byte[] hash,
            string installerArguments,
            CancellationToken ct = default,
            SetProgress setProgress = default)
        {
            using (var ctx = new CGoContext(ct))
            {
                var e = download_and_install_selfupdate(
                    string.Join("\0", uris.Select(u => u.AbsoluteUri).ToArray()) + "\0",
                    hash,
                    installerArguments,
                    ctx.Handle,
                    setProgress);
                if (e == null)
                    return;
                throw new Exception(e);
            }
        }

        [DllImport("eduvpn_windows.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern CGoInt64Ptr get_last_update_timestamp(IntPtr ctx);

        public static DateTimeOffset GetLastUpdateTimestamp(CancellationToken ct = default)
        {
            using (var ctx = new CGoContext(ct))
            {
                var m = CGoToManagedStringMarshaller.GetInstance(null);
                var r = get_last_update_timestamp(ctx.Handle);
                try
                {
                    if (r.r1 != IntPtr.Zero)
                        throw new Exception((string)m.MarshalNativeToManaged(r.r1));
                    return Epoch.AddSeconds(r.r0);
                }
                finally
                {
                    m.CleanUpNativeData(r.r1);
                }
            }
        }

        #endregion
    }
}

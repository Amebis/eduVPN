/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace eduVPN
{
    /// <summary>
    /// eduvpn-common engine interop
    /// </summary>
    public class Engine
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

            [DllImport("eduvpn_common.dll", CallingConvention = CallingConvention.Cdecl)]
            static extern void FreeString(IntPtr addr);

            /// <summary>
            /// Performs necessary cleanup of the unmanaged data when it is no longer needed.
            /// </summary>
            /// <param name="pNativeData">A pointer to the unmanaged data to be destroyed.</param>
            public void CleanUpNativeData(IntPtr pNativeData)
            {
                if (pNativeData != IntPtr.Zero)
                    FreeString(pNativeData);
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
        /// FSM state Base
        /// </summary>
        /// <see cref="eduvpn-common/client/fsm.go"/>
        public enum State
        {
            /// <summary>
            /// StateDeregistered means the app is not registered with the wrapper.
            /// </summary>
            Deregistered,

            /// <summary>
            /// StateNoServer means the user has not chosen a server yet.
            /// </summary>
            NoServer,

            /// <summary>
            /// StateAskLocation means the user selected a Secure Internet server but needs to choose a location.
            /// </summary>
            AskLocation,

            /// <summary>
            /// StateChosenLocation means the user has selected a Secure Internet location
            /// </summary>
            ChosenLocation,

            /// <summary>
            /// StateLoadingServer means we are loading the server details.
            /// </summary>
            LoadingServer,

            /// <summary>
            /// StateChosenServer means the user has chosen a server to connect to and the server is initialized.
            /// </summary>
            ChosenServer,

            /// <summary>
            /// StateOAuthStarted means the OAuth process has started.
            /// </summary>
            OAuthStarted,

            /// <summary>
            /// StateAuthorized means the OAuth process has finished and the user is now authorized with the server.
            /// </summary>
            Authorized,

            /// <summary>
            /// StateRequestConfig means the user has requested a config for connecting.
            /// </summary>
            RequestConfig,

            /// <summary>
            /// StateAskProfile means the go code is asking for a profile selection from the UI.
            /// </summary>
            AskProfile,

            /// <summary>
            /// StateChosenProfile means a profile has been chosen
            /// </summary>
            ChosenProfile,

            /// <summary>
            /// StateGotConfig means a VPN configuration has been obtained
            /// </summary>
            GotConfig,
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int StateCB(
            /*[MarshalAs(UnmanagedType.I4)]*/ State oldstate,
            /*[MarshalAs(UnmanagedType.I4)]*/ State newstate,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string data);

        /// <summary>
        /// Callback event arguments
        /// </summary>
        public class CallbackEventArgs : EventArgs
        {
            #region Fields

            /// <summary>
            /// Engine FSM old state
            /// </summary>
            public readonly State OldState;

            /// <summary>
            /// Engine FSM new state
            /// </summary>
            public readonly State NewState;

            /// <summary>
            /// Engine FSM state transition data
            /// </summary>
            public readonly string Data;

            /// <summary>
            /// Must be set to <c>true</c> on return
            /// </summary>
            public bool Handled;

            #endregion

            #region Constructors

            /// <summary>
            /// Constructs an event arguments
            /// </summary>
            /// <param name="oldState">Engine FSM old state</param>
            /// <param name="newState">Engine FSM new state</param>
            /// <param name="data">Engine FSM state transition data</param>
            public CallbackEventArgs(State oldState, State newState, string data)
            {
                OldState = oldState;
                NewState = newState;
                Data = data;
            }

            #endregion
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void TokenGetter(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string server,
            IntPtr outBuf,
            UIntPtr len);

        /// <summary>
        /// TokenGetter event arguments
        /// </summary>
        public class GetTokenEventArgs : EventArgs
        {
            #region Fields

            /// <summary>
            /// Server URI/Organization ID
            /// </summary>
            public readonly Uri Base;

            /// <summary>
            /// Must be set to token value on return
            /// </summary>
            public string Token;

            #endregion

            #region Constructors

            /// <summary>
            /// Constructs an event arguments
            /// </summary>
            /// <param name="_base">Server URI or Organization ID</param>
            public GetTokenEventArgs(Uri _base)
            {
                Base = _base;
            }

            #endregion
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void TokenSetter(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string server,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string token);

        /// <summary>
        /// TokenSetter event arguments
        /// </summary>
        public class SetTokenEventArgs : EventArgs
        {
            #region Fields

            /// <summary>
            /// Server URI/Organization ID
            /// </summary>
            public readonly Uri Base;

            /// <summary>
            /// Token value
            /// </summary>
            public readonly string Token;

            #endregion

            #region Constructors

            /// <summary>
            /// Constructs an event arguments
            /// </summary>
            /// <param name="_base">Server URI or Organization ID</param>
            /// <param name="token">Token</param>
            public SetTokenEventArgs(Uri _base, string token)
            {
                Base = _base;
                Token = token;
            }

            #endregion
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate long ReadRxBytes();

        /// <summary>
        /// ReportTraffic event arguments
        /// </summary>
        public class ReportTrafficEventArgs : EventArgs
        {
            #region Fields

            /// <summary>
            /// Must be set to number of bytes received
            /// </summary>
            public long RxBytes;

            /// <summary>
            /// Must be set to number of bytes sent
            /// </summary>
            public long TxBytes;

            #endregion
        }

        /// <summary>
        /// Cookie representing cancellable operation
        /// </summary>
        public class Cookie : IDisposable
        {
            #region Properties

            /// <summary>
            /// eduvpn-common cookie handle
            /// </summary>
            public IntPtr Handle { get; }

            #endregion

            #region Constructors

            [DllImport("eduvpn_common.dll", CallingConvention = CallingConvention.Cdecl)]
            static extern IntPtr CookieNew();

            public Cookie()
            {
                Handle = CookieNew();
            }

            #endregion

            #region Methods

            [DllImport("eduvpn_common.dll", CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
            static extern string CookieReply(
                IntPtr ctx,
                [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

            /// <summary>
            /// Replies to cookie request
            /// </summary>
            /// <param name="value">Value of the reply</param>
            /// <exception cref="Exception">operation failed</exception>
            public void Reply(string value)
            {
                var e = CookieReply(Handle, value);
                if (e == null)
                    return;
                throw new Exception(e);
            }

            [DllImport("eduvpn_common.dll", CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
            static extern string CookieCancel(IntPtr ctx);

            /// <summary>
            /// Cancels operation represented by the cookie
            /// </summary>
            /// <exception cref="Exception">operation failed</exception>
            public void Cancel()
            {
                var e = CookieCancel(Handle);
                if (e == null)
                    return;
                throw new Exception(e);
            }

            #endregion

            #region IDisposable Support
            /// <summary>
            /// Flag to detect redundant <see cref="Dispose(bool)"/> calls.
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            bool disposedValue = false;

            [DllImport("eduvpn_common.dll", CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
            static extern string CookieDelete(IntPtr ctx);

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
                        CookieDelete(Handle);
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
        /// Cookie representing cancellable operation, cancellation is bound to a CancellationToken
        /// </summary>
        public class CancellationTokenCookie : Cookie
        {
            #region Fields

            readonly CancellationTokenRegistration CancellationTokenRegistration;

            #endregion

            #region Constructors

            public CancellationTokenCookie(CancellationToken ct = default) : base()
            {
                CancellationTokenRegistration = ct.Register(() => Cancel());
            }

            #endregion

            #region IDisposable Support
            /// <summary>
            /// Flag to detect redundant <see cref="Dispose(bool)"/> calls.
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            bool disposedValue = false;

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                        CancellationTokenRegistration.Dispose();
                    disposedValue = true;
                }
                base.Dispose(disposing);
            }
            #endregion
        }

        #endregion

        #region Properties

        /// <summary>
        /// Occurs on engine events
        /// </summary>
        public static event EventHandler<CallbackEventArgs> Callback;

        /// <summary>
        /// Occurs on token get
        /// </summary>
        public static event EventHandler<GetTokenEventArgs> GetToken;

        /// <summary>
        /// Occurs on token set
        /// </summary>
        public static event EventHandler<SetTokenEventArgs> SetToken;

        /// <summary>
        /// Called from engine to retrieve the number of bytes read over the VPN tunnel
        /// </summary>
        public static event EventHandler<ReportTrafficEventArgs> ReportTraffic;

        #endregion

        #region Methods

        static readonly StateCB OnEngineStateChanged = new StateCB((State oldstate, State newstate, string data) =>
        {
            if (Callback != null)
            {
                var args = new CallbackEventArgs(oldstate, newstate, data);
                Callback.Invoke(null, args);
                if (args.Handled)
                    return 1;
            }
            return 0;
        });

        [DllImport("eduvpn_common.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string Register(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string version,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string configDirectory,
            StateCB stateCallback,
            int debug);

        [DllImport("eduvpn_common.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string SetSupportWireguard(int support);

        static readonly TokenGetter OnGetToken = new TokenGetter((string server, IntPtr outBuf, UIntPtr len) =>
        {
            if (eduJSON.Parser.Parse(server) is IReadOnlyDictionary<string, object> obj)
            {
                var srv = Server.Load(obj);
                var args = new GetTokenEventArgs(srv.Base);
                GetToken?.Invoke(null, args);
                if (args.Token != null)
                {
                    var data = Encoding.UTF8.GetBytes(args.Token);
                    if (data.Length < (int)len)
                    {
                        Marshal.Copy(data, 0, outBuf, data.Length);
                        Marshal.WriteByte(outBuf, data.Length, 0);
                    }
                    else
                        Marshal.Copy(data, 0, outBuf, (int)len);
                    return;
                }
            }
            if ((ulong)len > 0)
                Marshal.WriteByte(outBuf, 0, 0);
        });

        static readonly TokenSetter OnSetToken = new TokenSetter((string server, string token) =>
        {
            if (eduJSON.Parser.Parse(server) is IReadOnlyDictionary<string, object> obj)
            {
                var srv = Server.Load(obj);
                SetToken?.Invoke(null, new SetTokenEventArgs(srv.Base, token));
            }
        });

        [DllImport("eduvpn_common.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string SetTokenHandler(TokenGetter getter, TokenSetter setter);

        /// <summary>
        /// Register initializes the client.
        /// </summary>
        /// <exception cref="Exception">Initialization failed, for example when discovery cannot be obtained and when there are no servers</exception>
        public static void Register()
        {
            var e = Register(
                Properties.Settings.Default.ClientId + ".windows",
                Assembly.GetExecutingAssembly()?.GetName()?.Version.ToString(),
                Path.GetDirectoryName(Path.GetDirectoryName(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath)),
                OnEngineStateChanged,
#if DEBUG
                1
#else
                0
#endif
                );
            if (e != null)
                throw new Exception(e);
            e = SetSupportWireguard(1);
            if (e != null)
                throw new Exception(e);
            e = SetTokenHandler(OnGetToken, OnSetToken);
            if (e != null)
                throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "Deregister", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string _Deregister();

        /// <summary>
        /// Deregisters the client, meaning saving the log file and the config and emptying out the client struct.
        /// </summary>
        /// <exception cref="Exception">Call failed</exception>
        public static void Deregister()
        {
            var e = _Deregister();
            if (e == null)
                return;
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "ExpiryTimes", CallingConvention = CallingConvention.Cdecl)]
        static extern CGoPtrPtr _ExpiryTimes();

        /// <summary>
        /// Returns the different Unix timestamps regarding expiry.
        /// - The time starting at which the renew button should be shown, after 30 minutes and less than 24 hours
        /// - The time starting at which the countdown button should be shown, less than 24 hours
        /// - The list of times where notifications should be shown
        /// These times are reset when the VPN gets disconnected.
        /// </summary>
        /// <returns>JSON string</returns>
        /// <exception cref="Exception">Call failed</exception>
        public static string ExpiryTimes()
        {
            var r = _ExpiryTimes();
            var m = CGoToManagedStringMarshaller.GetInstance(null);
            if (r.r1 == IntPtr.Zero)
            {
                var v = (string)m.MarshalNativeToManaged(r.r0);
                m.CleanUpNativeData(r.r0);
                return v;
            }
            var e = (string)m.MarshalNativeToManaged(r.r1);
            m.CleanUpNativeData(r.r0);
            m.CleanUpNativeData(r.r1);
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "AddServer", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string _AddServer(
            IntPtr c,
            /*[MarshalAs(UnmanagedType.I4)]*/ ServerType type,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string id,
            int ni);

        /// <summary>
        /// Adds server
        /// </summary>
        /// <param name="cookie">eduvpn-common operation cookie</param>
        /// <param name="type">Server type</param>
        /// <param name="id">Server base URL/ID</param>
        /// <param name="quiet">Should adding skip OAuth?</param>
        /// <exception cref="OperationCanceledException">Call cancelled</exception>
        /// <exception cref="Exception">Call failed</exception>
        public static void AddServer(Cookie cookie, ServerType type, string id, bool quiet)
        {
            var e = _AddServer(cookie.Handle, type, id, quiet ? 1 : 0);
            if (e == null)
                return;
            if (e.EndsWith(": context canceled"))
                throw new OperationCanceledException();
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "RemoveServer", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string _RemoveServer(
            /*[MarshalAs(UnmanagedType.I4)]*/ ServerType type,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string id);

        /// <summary>
        /// Removes server
        /// </summary>
        /// <param name="type">Server type</param>
        /// <param name="id">Server base URL/ID</param>
        /// <exception cref="Exception">Call failed</exception>
        public static void RemoveServer(ServerType type, string id)
        {
            var e = _RemoveServer(type, id);
            if (e == null || e.EndsWith(", not found in list"))
                return;
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "CurrentServer", CallingConvention = CallingConvention.Cdecl)]
        static extern CGoPtrPtr _CurrentServer();

        public static string CurrentServer()
        {
            var r = _CurrentServer();
            var m = CGoToManagedStringMarshaller.GetInstance(null);
            if (r.r1 == IntPtr.Zero)
            {
                var v = (string)m.MarshalNativeToManaged(r.r0);
                m.CleanUpNativeData(r.r0);
                return v;
            }
            var e = (string)m.MarshalNativeToManaged(r.r1);
            m.CleanUpNativeData(r.r0);
            m.CleanUpNativeData(r.r1);
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "ServerList", CallingConvention = CallingConvention.Cdecl)]
        static extern CGoPtrPtr _ServerList();

        public static string ServerList()
        {
            var r = _ServerList();
            var m = CGoToManagedStringMarshaller.GetInstance(null);
            if (r.r1 == IntPtr.Zero)
            {
                var v = (string)m.MarshalNativeToManaged(r.r0);
                m.CleanUpNativeData(r.r0);
                return v;
            }
            var e = (string)m.MarshalNativeToManaged(r.r1);
            m.CleanUpNativeData(r.r0);
            m.CleanUpNativeData(r.r1);
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "GetConfig", CallingConvention = CallingConvention.Cdecl)]
        static extern CGoPtrPtr _GetConfig(
            IntPtr c,
            /*[MarshalAs(UnmanagedType.I4)]*/ ServerType type,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string id,
            int pTCP);

        /// <summary>
        /// Gets a configuration for a server
        /// It ensures that the Institute Access Server exists by creating or using an existing one with the url.
        /// </summary>
        /// <param name="cookie">eduvpn-common operation cookie</param>
        /// <param name="type">Server type</param>
        /// <param name="id">Server base URL/ID</param>
        /// <param name="tcp">indicates that the client wants to use TCP (through OpenVPN) to establish the VPN tunnel</param>
        /// <returns>JSON string</returns>
        /// <exception cref="OperationCanceledException">Call cancelled</exception>
        /// <exception cref="Exception">Call failed</exception>
        public static string GetConfig(Cookie cookie, ServerType type, string id, bool tcp)
        {
            var r = _GetConfig(cookie.Handle, type, id, tcp ? 1 : 0);
            var m = CGoToManagedStringMarshaller.GetInstance(null);
            if (r.r1 == IntPtr.Zero)
            {
                var v = (string)m.MarshalNativeToManaged(r.r0);
                m.CleanUpNativeData(r.r0);
                return v;
            }
            var e = (string)m.MarshalNativeToManaged(r.r1);
            m.CleanUpNativeData(r.r0);
            m.CleanUpNativeData(r.r1);
            if (e.EndsWith(": context canceled"))
                throw new OperationCanceledException();
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "SetProfileID", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string _SetProfileId([MarshalAs(UnmanagedType.LPUTF8Str)] string profileId);

        /// <summary>
        /// Sets a profile ID for the current server.
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <exception cref="Exception">Call failed</exception>
        public static void SetProfileId(string profileId)
        {
            var e = _SetProfileId(profileId);
            if (e == null)
                return;
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "SetSecureLocation", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string _SetSecureLocation(
            IntPtr c,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string countryCode);

        /// <summary>
        /// Sets the location for the current secure internet server. countryCode is the secure location to be chosen.
        /// </summary>
        /// <param name="cookie">eduvpn-common operation cookie</param>
        /// <param name="countryCode">The secure location to be chosen</param>
        /// <exception cref="OperationCanceledException">Call cancelled</exception>
        /// <exception cref="Exception">Call failed</exception>
        public static void SetSecureInternetLocation(Cookie cookie, string countryCode)
        {
            var e = _SetSecureLocation(cookie.Handle, countryCode);
            if (e == null)
                return;
            if (e.EndsWith(": context canceled"))
                throw new OperationCanceledException();
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "DiscoServers", CallingConvention = CallingConvention.Cdecl)]
        static extern CGoPtrPtr _DiscoServers(IntPtr c);

        /// <summary>
        /// Gets the servers list from the discovery server.
        /// In case of error, a previous version of the list is returned if there is any.
        /// This takes into account the frequency of updates, see: https://github.com/eduvpn/documentation/blob/v3/SERVER_DISCOVERY.md#server-list.
        /// </summary>
        /// <returns>JSON string</returns>
        /// <param name="cookie">eduvpn-common operation cookie</param>
        /// <exception cref="OperationCanceledException">Call cancelled</exception>
        /// <exception cref="Exception">Call failed</exception>
        public static string DiscoServers(Cookie cookie)
        {
            var r = _DiscoServers(cookie.Handle);
            var m = CGoToManagedStringMarshaller.GetInstance(null);
            if (r.r1 == IntPtr.Zero)
            {
                var v = (string)m.MarshalNativeToManaged(r.r0);
                m.CleanUpNativeData(r.r0);
                return v;
            }
            var e = (string)m.MarshalNativeToManaged(r.r1);
            m.CleanUpNativeData(r.r0);
            m.CleanUpNativeData(r.r1);
            if (e.EndsWith(": context canceled"))
                throw new OperationCanceledException();
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "DiscoOrganizations", CallingConvention = CallingConvention.Cdecl)]
        static extern CGoPtrPtr _DiscoOrganizations(IntPtr c);

        /// <summary>
        /// Gets the organizations list from the discovery server.
        /// In case of error, a previous version of the list is returned if there is any.
        /// This takes into account the frequency of updates, see: https://github.com/eduvpn/documentation/blob/v3/SERVER_DISCOVERY.md#organization-list.
        /// </summary>
        /// <param name="cookie">eduvpn-common operation cookie</param>
        /// <returns>JSON string</returns>
        /// <exception cref="OperationCanceledException">Call cancelled</exception>
        /// <exception cref="Exception">Call failed</exception>
        public static string DiscoOrganizations(Cookie cookie)
        {
            var r = _DiscoOrganizations(cookie.Handle);
            var m = CGoToManagedStringMarshaller.GetInstance(null);
            if (r.r1 == IntPtr.Zero)
            {
                var v = (string)m.MarshalNativeToManaged(r.r0);
                m.CleanUpNativeData(r.r0);
                return v;
            }
            var e = (string)m.MarshalNativeToManaged(r.r1);
            m.CleanUpNativeData(r.r0);
            m.CleanUpNativeData(r.r1);
            if (e.EndsWith(": context canceled"))
                throw new OperationCanceledException();
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "Cleanup", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string _Cleanup(IntPtr c);

        /// <summary>
        /// Cleans up the VPN connection by sending a /disconnect to the server
        /// </summary>
        /// <param name="cookie">eduvpn-common operation cookie</param>
        /// <exception cref="OperationCanceledException">Call cancelled</exception>
        /// <exception cref="Exception">Call failed</exception>
        public static void Cleanup(Cookie cookie)
        {
            var e = _Cleanup(cookie.Handle);
            if (e == null)
                return;
            if (e.EndsWith(": context canceled"))
                throw new OperationCanceledException();
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "RenewSession", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string _RenewSession(IntPtr c);

        /// <summary>
        /// Renews the session for the current VPN server.
        /// </summary>
        /// <param name="cookie">eduvpn-common operation cookie</param>
        /// <exception cref="OperationCanceledException">Call cancelled</exception>
        /// <exception cref="Exception">Call failed</exception>
        public static void RenewSession(Cookie cookie)
        {
            var e = _RenewSession(cookie.Handle);
            if (e == null)
                return;
            if (e.EndsWith(": context canceled"))
                throw new OperationCanceledException();
            throw new Exception(e);
        }

        static readonly ReadRxBytes OnRxBytesRead = new ReadRxBytes(() =>
        {
            if (ReportTraffic != null)
            {
                var args = new ReportTrafficEventArgs();
                ReportTraffic.Invoke(null, args);
                return args.RxBytes;
            }
            return 0;
        });

        [DllImport("eduvpn_common.dll", EntryPoint = "StartFailover", CallingConvention = CallingConvention.Cdecl)]
        static extern CGoIntPtr _StartFailover(
            IntPtr c,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string gateway,
            int mtu,
            ReadRxBytes readRxBytes);

        /// <summary>
        /// Starts VPN connection test
        /// </summary>
        /// <param name="cookie">eduvpn-common operation cookie</param>
        /// <param name="gateway">IPv4 address of the gateway to ping</param>
        /// <param name="mtu">Ping MTU</param>
        /// <returns>true if all packets were dropped; false if at least some RX traffic detected</returns>
        /// <exception cref="OperationCanceledException">Call cancelled</exception>
        /// <exception cref="Exception">Call failed</exception>
        public static bool StartFailover(Cookie cookie, string gateway, int mtu)
        {
            var r = _StartFailover(cookie.Handle, gateway, mtu, OnRxBytesRead);
            var m = CGoToManagedStringMarshaller.GetInstance(null);
            if (r.r1 == IntPtr.Zero)
                return r.r0 != 0;
            var e = (string)m.MarshalNativeToManaged(r.r1);
            m.CleanUpNativeData(r.r1);
            if (e.EndsWith(": context canceled"))
                throw new OperationCanceledException();
            throw new Exception(e);
        }

        #endregion
    }
}

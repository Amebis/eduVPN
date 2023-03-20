/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

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
            private static extern void FreeString(IntPtr addr);

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
        /// FSM state Id
        /// </summary>
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
            /// Must be set to true to indicate the callback was handled
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

        /// <summary>
        /// The type/role of eduVPN server
        /// </summary>
        enum ServerType
        {
            /// <summary>
            /// The server is unknown
            /// </summary>
            Unknown,

            /// <summary>
            /// The server is of type Institute Access
            /// </summary>
            InstituteAccess,

            /// <summary>
            /// The server is of type Secure Internet
            /// </summary>
            SecureInternet,

            /// <summary>
            /// The server is own server
            /// </summary>
            Custom,
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

        #endregion

        #region Properties

        /// <summary>
        /// Occurs on engine events
        /// </summary>
        public static event EventHandler<CallbackEventArgs> Callback;

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
                return args.Handled ? 1 : 0;
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

        /// <summary>
        /// Register initializes the client.
        /// </summary>
        /// <exception cref="Exception">Initialization failed, for example when discovery cannot be obtained and when there are no servers</exception>
        public static void Register()
        {
            var e = Register(
                Properties.Settings.Default.ClientId + ".windows",
                Assembly.GetExecutingAssembly()?.GetName()?.Version.ToString(),
                Path.GetDirectoryName(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath),
                OnEngineStateChanged,
#if DEBUG
                1
#else
                0
#endif
                );
            if (e == null)
                return;
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
        static extern CGoPtrPair _ExpiryTimes();

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

        [DllImport("eduvpn_common.dll", EntryPoint = "CancelOAuth", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string _CancelOAuth();

        /// <summary>
        /// Cancels OAuth if one is in progress.
        /// </summary>
        /// <exception cref="Exception">Call failed</exception>
        public static void CancelOAuth()
        {
            var e = _CancelOAuth();
            if (e == null)
                return;
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string AddServer(
            /*[MarshalAs(UnmanagedType.I4)]*/ ServerType type,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string id);

        /// <summary>
        /// Adds Institute Access server
        /// </summary>
        /// <param name="url">Server base URL</param>
        /// <exception cref="Exception">Call failed</exception>
        public static void AddInstituteAccessServer(Uri url)
        {
            var e = AddServer(ServerType.InstituteAccess, url.AbsoluteUri);
            if (e == null)
                return;
            if (e == "client cancelled OAuth")
                throw new OperationCanceledException();
            throw new Exception(e);
        }

        /// <summary>
        /// Adds Secure Internet home server
        /// </summary>
        /// <param name="orgId">Organization ID</param>
        /// <exception cref="Exception">Call failed</exception>
        public static void AddSecureInternetHomeServer(string orgId)
        {
            var e = AddServer(ServerType.SecureInternet, orgId);
            if (e == null)
                return;
            if (e == "client cancelled OAuth")
                throw new OperationCanceledException();
            throw new Exception(e);
        }

        /// <summary>
        /// Adds own server
        /// </summary>
        /// <param name="url">Server base URL</param>
        /// <exception cref="Exception">Call failed</exception>
        public static void AddOwnServer(Uri url)
        {
            var e = AddServer(ServerType.Custom, url.AbsoluteUri);
            if (e == null)
                return;
            if (e == "client cancelled OAuth")
                throw new OperationCanceledException();
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string RemoveServer(
            /*[MarshalAs(UnmanagedType.I4)]*/ ServerType type,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string id);

        /// <summary>
        /// Removes Institute Access server
        /// </summary>
        /// <param name="url">Server base URL</param>
        /// <exception cref="Exception">Call failed</exception>
        public static void RemoveInstituteAccessServer(Uri url)
        {
            var e = RemoveServer(ServerType.InstituteAccess, url.AbsoluteUri);
            if (e == null)
                return;
            throw new Exception(e);
        }

        /// <summary>
        /// Removes Secure Internet home server
        /// </summary>
        /// <exception cref="Exception">Call failed</exception>
        public static void RemoveSecureInternetHomeServer()
        {
            var e = RemoveServer(ServerType.SecureInternet, null);
            if (e == null)
                return;
            throw new Exception(e);
        }

        /// <summary>
        /// Removes own server
        /// </summary>
        /// <param name="url">Server base URL</param>
        /// <exception cref="Exception">Call failed</exception>
        public static void RemoveOwnServer(Uri url)
        {
            var e = RemoveServer(ServerType.Custom, url.AbsoluteUri);
            if (e == null)
                return;
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "CurrentServer", CallingConvention = CallingConvention.Cdecl)]
        static extern CGoPtrPair _CurrentServer();

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
        static extern CGoPtrPair _ServerList();

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

        [DllImport("eduvpn_common.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern CGoPtrPair GetConfig(
            /*[MarshalAs(UnmanagedType.I4)]*/ ServerType type,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string id,
            int pTCP,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string tokens);

        /// <summary>
        /// Gets a configuration for an Institute Access Server
        /// It ensures that the Institute Access Server exists by creating or using an existing one with the url.
        /// </summary>
        /// <param name="url">Server base URL</param>
        /// <param name="tcp">indicates that the client wants to use TCP (through OpenVPN) to establish the VPN tunnel</param>
        /// <param name="tokens">OAuth tokens</param>
        /// <returns>JSON string</returns>
        /// <exception cref="Exception">Call failed</exception>
        public static string GetInstituteAccessConfig(Uri url, bool tcp, string tokens)
        {
            var r = GetConfig(ServerType.InstituteAccess, url.AbsoluteUri, tcp ? 1 : 0, tokens);
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

        /// <summary>
        /// Gets a configuration for a Secure Internet Server
        /// </summary>
        /// <param name="tcp">indicates that the client wants to use TCP (through OpenVPN) to establish the VPN tunnel</param>
        /// <param name="tokens">OAuth tokens</param>
        /// <returns>JSON string</returns>
        /// <exception cref="Exception">Call failed</exception>
        public static string GetSecureInternetConfig(bool tcp, string tokens)
        {
            var r = GetConfig(ServerType.SecureInternet, null, tcp ? 1 : 0, tokens);
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

        /// <summary>
        /// Gets a configuration for a Secure Internet Server
        /// It ensures that the Custom Server exists by creating or using an existing one with the url.
        /// </summary>
        /// <param name="url">Server base URL</param>
        /// <param name="tcp">indicates that the client wants to use TCP (through OpenVPN) to establish the VPN tunnel</param>
        /// <param name="tokens">OAuth tokens</param>
        /// <returns>JSON string</returns>
        /// <exception cref="Exception">Call failed</exception>
        public static string GetOwnConfig(Uri url, bool tcp, string tokens)
        {
            var r = GetConfig(ServerType.Custom, url.AbsoluteUri, tcp ? 1 : 0, tokens);
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
        static extern string _SetSecureLocation([MarshalAs(UnmanagedType.LPUTF8Str)] string countryCode);

        /// <summary>
        /// Sets the location for the current secure internet server. countryCode is the secure location to be chosen.
        /// </summary>
        /// <param name="countryCode">The secure location to be chosen</param>
        /// <exception cref="Exception">Call failed</exception>
        public static void SetSecureInternetLocation(string countryCode)
        {
            var e = _SetSecureLocation(countryCode);
            if (e == null)
                return;
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "DiscoServers", CallingConvention = CallingConvention.Cdecl)]
        static extern CGoPtrPair _DiscoServers();

        /// <summary>
        /// Gets the servers list from the discovery server.
        /// In case of error, a previous version of the list is returned if there is any.
        /// This takes into account the frequency of updates, see: https://github.com/eduvpn/documentation/blob/v3/SERVER_DISCOVERY.md#server-list.
        /// </summary>
        /// <returns>JSON string</returns>
        /// <exception cref="Exception">Call failed</exception>
        public static string DiscoServers()
        {
            var r = _DiscoServers();
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

        [DllImport("eduvpn_common.dll", EntryPoint = "DiscoOrganizations", CallingConvention = CallingConvention.Cdecl)]
        static extern CGoPtrPair _DiscoOrganizations();

        /// <summary>
        /// Gets the organizations list from the discovery server.
        /// In case of error, a previous version of the list is returned if there is any.
        /// This takes into account the frequency of updates, see: https://github.com/eduvpn/documentation/blob/v3/SERVER_DISCOVERY.md#organization-list.
        /// </summary>
        /// <returns>JSON string</returns>
        /// <exception cref="Exception">Call failed</exception>
        public static string DiscoOrganizations()
        {
            var r = _DiscoOrganizations();
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

        [DllImport("eduvpn_common.dll", EntryPoint = "Cleanup", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string _Cleanup([MarshalAs(UnmanagedType.LPUTF8Str)] string prevTokens);

        /// <summary>
        /// Cleans up the VPN connection by sending a /disconnect to the server
        /// </summary>
        /// <exception cref="Exception">Call failed</exception>
        public static void Cleanup(string prevTokens)
        {
            var e = _Cleanup(prevTokens);
            if (e == null)
                return;
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "RenewSession", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string _RenewSession();

        /// <summary>
        /// Renews the session for the current VPN server.
        /// </summary>
        /// <exception cref="Exception">Call failed</exception>
        public static void RenewSession()
        {
            var e = _RenewSession();
            if (e == null)
                return;
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "SetSupportWireguard", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string _SetSupportWireGuard(int support);

        /// <summary>
        /// Declares WireGuard support.
        /// </summary>
        /// <param name="support">true if client supports WireGuard</param>
        /// <exception cref="Exception">Call failed</exception>
        public static void SetSupportWireGuard(bool support)
        {
            var e = _SetSupportWireGuard(support ? 1 : 0);
            if (e == null)
                return;
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "SecureLocationList", CallingConvention = CallingConvention.Cdecl)]
        static extern CGoPtrPair _SecureLocationList();

        /// <summary>
        /// Returns all the available locations
        /// </summary>
        /// <returns>JSON string</returns>
        /// <exception cref="Exception">Call failed</exception>
        public static string SecureInternetLocationList()
        {
            var r = _SecureLocationList();
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
            [MarshalAs(UnmanagedType.LPUTF8Str)] string gateway,
            int mtu,
            ReadRxBytes readRxBytes);

        /// <summary>
        /// Starts VPN connection test
        /// </summary>
        /// <returns>true if all packets were dropped; false if at least some RX traffic detected</returns>
        /// <exception cref="Exception">Call failed</exception>
        public static bool StartFailover(string gateway, int mtu)
        {
            var r = _StartFailover(gateway, mtu, OnRxBytesRead);
            var m = CGoToManagedStringMarshaller.GetInstance(null);
            if (r.r1 == IntPtr.Zero)
                return r.r0 != 0;
            var e = (string)m.MarshalNativeToManaged(r.r1);
            m.CleanUpNativeData(r.r1);
            throw new Exception(e);
        }

        [DllImport("eduvpn_common.dll", EntryPoint = "CancelFailover", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CGoToManagedStringMarshaller))]
        static extern string _CancelFailover();

        /// <summary>
        /// Cancels VPN connection test
        /// </summary>
        /// <exception cref="Exception">Call failed</exception>
        public static void CancelFailover()
        {
            var e = _CancelFailover();
            if (e == null)
                return;
            throw new Exception(e);
        }

        #endregion
    }
}

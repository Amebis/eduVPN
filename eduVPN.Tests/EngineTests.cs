/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Timers;

namespace eduVPN.Tests
{
    [TestClass()]
    public class EngineTests
    {
        void InitSettings()
        {
            Properties.SettingsEx.Default.RegistryKeyPath = @"SOFTWARE\SURF\eduVPN";
            Properties.Settings.Default.SelfUpdateBundleId = "{EF5D5806-B90B-4AA3-800A-2D7EA1592BA0}";
            Properties.Settings.Default.ClientId = "org.eduvpn.app";
            Properties.Settings.Default.ClientTitle = "eduVPN";
            Properties.Settings.Default.ClientSimpleName = "eduVPN";
            Properties.Settings.Default.ClientAboutUri = new Uri(@"https://www.eduvpn.org/");
        }

        [TestMethod()]
        public void RegisterTest()
        {
            InitSettings();
            int callbackCounter = 0;
            Engine.Callback += (object sender, Engine.CallbackEventArgs e) =>
            {
                switch (callbackCounter++)
                {
                    case 0:
                        Assert.AreEqual(Engine.State.Deregistered, e.OldState);
                        Assert.AreEqual(Engine.State.NoServer, e.NewState);
                        e.Handled = true;
                        break;
                }
            };
            Engine.Register();
            Engine.Deregister();
            Assert.AreEqual(1, callbackCounter);

            Properties.Settings.Default.ClientId = "net.bar.foo";
            Assert.ThrowsException<Exception>(() => Engine.Register());
        }

        [TestMethod()]
        public void ExpiryTest()
        {
            InitSettings();
            Engine.Register();
            try
            {
                Assert.ThrowsException<Exception>(() => Engine.ExpiryTimes());
            }
            finally
            {
                Engine.Deregister();
            }
        }

        [TestMethod()]
        public void CancelOAuthTest()
        {
            InitSettings();
            Engine.Register();
            try
            {
                Assert.ThrowsException<Exception>(() => Engine.CancelOAuth());
            }
            finally
            {
                Engine.Deregister();
            }
        }

        [TestMethod()]
        public void AddServerTest()
        {
            InitSettings();
            Engine.Register();
            try
            {
                Assert.ThrowsException<Exception>(() => Engine.AddOwnServer(new Uri("https://vpn.tuxed.net")));
                int callbackCounter = 0;
                Engine.Callback += (object sender, Engine.CallbackEventArgs e) =>
                {
                    switch (callbackCounter++)
                    {
                        case 0:
                            Assert.AreEqual(Engine.State.NoServer, e.OldState);
                            Assert.AreEqual(Engine.State.LoadingServer, e.NewState);
                            e.Handled = true;
                            break;
                        case 1:
                            Assert.AreEqual(Engine.State.LoadingServer, e.OldState);
                            Assert.AreEqual(Engine.State.ChosenServer, e.NewState);
                            e.Handled = true;
                            break;
                        case 2:
                            Assert.AreEqual(Engine.State.ChosenServer, e.OldState);
                            Assert.AreEqual(Engine.State.OAuthStarted, e.NewState);
                            Assert.IsNotNull(e.Data);
                            var timer = new System.Timers.Timer(200) { AutoReset = false };
                            timer.Elapsed += (object sender2, ElapsedEventArgs e2) => Engine.CancelOAuth();
                            timer.Start();
                            e.Handled = true;
                            break;
                        case 3:
                            Assert.AreEqual(Engine.State.OAuthStarted, e.OldState);
                            Assert.AreEqual(Engine.State.NoServer, e.NewState);
                            break;
                        case 4:
                            Assert.AreEqual(Engine.State.NoServer, e.OldState);
                            Assert.AreEqual(Engine.State.NoServer, e.NewState);
                            break;
                    }
                };
                Assert.ThrowsException<OperationCanceledException>(() => Engine.AddOwnServer(new Uri("https://vpn.tuxed.net")));
                Assert.AreEqual(5, callbackCounter);
            }
            finally
            {
                Engine.Deregister();
            }
        }

        [TestMethod()]
        public void RemoveServerTest()
        {
            InitSettings();
            Engine.Register();
            try
            {
                Engine.RemoveInstituteAccessServer(new Uri("https://vpn.tuxed.net"));
                Engine.RemoveSecureInternetHomeServer();
                Engine.RemoveOwnServer(new Uri("https://vpn.tuxed.net"));
            }
            finally
            {
                Engine.Deregister();
            }

            Assert.ThrowsException<Exception>(() => Engine.RemoveInstituteAccessServer(new Uri("https://vpn.tuxed.net")));
            Assert.ThrowsException<Exception>(() => Engine.RemoveSecureInternetHomeServer());
            Assert.ThrowsException<Exception>(() => Engine.RemoveOwnServer(new Uri("https://vpn.tuxed.net")));
        }

        [TestMethod()]
        public void CurrentServerTest()
        {
            InitSettings();
            Engine.Register();
            try
            {
                Assert.ThrowsException<Exception>(() => Engine.CurrentServer());
            }
            finally
            {
                Engine.Deregister();
            }
        }

        [TestMethod()]
        public void ServerListTest()
        {
            InitSettings();
            Engine.Register();
            try
            {
                CollectionAssert.AreEqual(new Dictionary<string, object>(), (Dictionary<string, object>)eduJSON.Parser.Parse(Engine.ServerList()));
            }
            finally
            {
                Engine.Deregister();
            }

            Assert.ThrowsException<Exception>(() => Engine.ServerList());
        }

        [TestMethod()]
        public void SetProfileIdTest()
        {
            InitSettings();
            Engine.Register();
            try
            {
                Assert.ThrowsException<Exception>(() => Engine.SetProfileId("foo bar"));
            }
            finally
            {
                Engine.Deregister();
            }
        }

        [TestMethod()]
        public void SetSecureLocationTest()
        {
            InitSettings();
            Engine.Register();
            try
            {
                Assert.ThrowsException<Exception>(() => Engine.SetSecureInternetLocation("de"));
            }
            finally
            {
                Engine.Deregister();
            }
        }

        [TestMethod()]
        public void DiscoveryTest()
        {
            InitSettings();
            Engine.Register();
            try
            {
                eduJSON.Parser.Parse(Engine.DiscoServers());
                eduJSON.Parser.Parse(Engine.DiscoOrganizations());
                eduJSON.Parser.Parse(Engine.SecureInternetLocationList());
            }
            finally
            {
                Engine.Deregister();
            }

            Assert.ThrowsException<Exception>(() => Engine.DiscoServers());
            Assert.ThrowsException<Exception>(() => Engine.DiscoOrganizations());
            Assert.ThrowsException<Exception>(() => Engine.SecureInternetLocationList());
        }

        [TestMethod()]
        public void CleanupTest()
        {
            InitSettings();
            Engine.Register();
            try
            {
                Assert.ThrowsException<Exception>(() => Engine.Cleanup("{}"));
            }
            finally
            {
                Engine.Deregister();
            }
        }

        [TestMethod()]
        public void RenewSessionTest()
        {
            InitSettings();
            Engine.Register();
            try
            {
                Assert.ThrowsException<Exception>(() => Engine.RenewSession());
            }
            finally
            {
                Engine.Deregister();
            }
        }

        [TestMethod()]
        public void SetSupportWireGuardTest()
        {
            InitSettings();
            Engine.Register();
            try
            {
                Engine.SetSupportWireGuard(true);
            }
            finally
            {
                Engine.Deregister();
            }

            Assert.ThrowsException<Exception>(() => Engine.SetSupportWireGuard(true));
        }

        [TestMethod()]
        public void FailoverTest()
        {
            InitSettings();
            Engine.Register();
            try
            {
                // TODO: openvpn-common engine checks eduVPN server profile if it supports OpenVPN protocol
                // and refuses to perform the failover test if not. If that check is commented, we can perform
                // failover test on any network connection using code below:
                //long rx = 0;
                //long tx = 0;
                //Engine.ReportTraffic += (object sender, Engine.ReportTrafficEventArgs e) =>
                //{
                //    e.RxBytes = rx;
                //    e.TxBytes = tx;
                //};
                //bool dropped = false;
                //Exception exception = null;
                //var t = new Thread(() =>
                //{
                //    try { dropped = Engine.StartFailover("172.217.19.100", 1450); } catch (Exception ex) { exception = ex; }
                //});
                //t.Start();
                //for (int i = 0; i < 50; i++)
                //{
                //    Thread.Sleep(50);
                //    rx += 100;
                //    tx += 100;
                //}
                //t.Join();
                //Assert.AreEqual(null, exception);
                //Assert.AreEqual(false, dropped);
                //rx = tx = 0;
                //t = new Thread(() =>
                //{
                //    try { dropped = Engine.StartFailover("172.217.19.100", 5000); } catch (Exception ex) { exception = ex; }
                //});
                //t.Start();
                //t.Join();
                //Assert.AreEqual(null, exception);
                //Assert.AreEqual(true, dropped);

                Assert.ThrowsException<Exception>(() => Engine.StartFailover("172.217.19.100", 1450));
            }
            finally
            {
                Engine.Deregister();
            }
        }
    }
}

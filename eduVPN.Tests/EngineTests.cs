/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;

namespace eduVPN.Tests
{
    [TestClass()]
    public class EngineTests
    {
        [TestMethod()]
        public void RegisterTest()
        {
            int callbackCounter = 0;
            Engine.Callback += (object sender, Engine.CallbackEventArgs e) =>
            {
                switch (callbackCounter++)
                {
                    case 0:
                        Assert.AreEqual(Engine.State.Main, e.OldState);
                        Assert.AreEqual(Engine.State.Main, e.NewState);
                        break;
                    case 1:
                        Assert.AreEqual(Engine.State.Main, e.OldState);
                        Assert.AreEqual(Engine.State.Deregistered, e.NewState);
                        break;
                }
            };
            Engine.Register();
            Engine.Deregister();
            Assert.AreEqual(2, callbackCounter);
        }

        [TestMethod()]
        public void ExpiryTest()
        {
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
        public void AddServerTest()
        {
            using (var cookie = new Engine.Cookie())
                Assert.ThrowsException<Exception>(() => Engine.AddServer(cookie, ServerType.Own, "https://vpn.tuxed.net", false));
            Engine.Register();
            using (var ct = new CancellationTokenSource())
            using (var cookie = new Engine.CancellationTokenCookie(ct.Token))
                try
                {
                    Assert.ThrowsException<Exception>(() => Engine.AddServer(cookie, ServerType.Own, "https://vpn.tuxed.net", false));
                    int callbackCounter = 0;
                    Engine.Callback += (object sender, Engine.CallbackEventArgs e) =>
                    {
                        switch (callbackCounter++)
                        {
                            case 0:
                                Assert.AreEqual(Engine.State.Main, e.OldState);
                                Assert.AreEqual(Engine.State.AddingServer, e.NewState);
                                break;
                            case 1:
                                Assert.AreEqual(Engine.State.AddingServer, e.OldState);
                                Assert.AreEqual(Engine.State.OAuthStarted, e.NewState);
                                Assert.IsNotNull(e.Data);
                                ct.CancelAfter(200);
                                break;
                            case 2:
                                Assert.AreEqual(Engine.State.OAuthStarted, e.OldState);
                                Assert.AreEqual(Engine.State.Main, e.NewState);
                                break;
                        }
                        e.Handled = true;
                    };
                    Assert.ThrowsException<OperationCanceledException>(() => Engine.AddServer(cookie, ServerType.Own, "https://vpn.tuxed.net", false));
                    Assert.AreEqual(3, callbackCounter);
                }
                finally
                {
                    Engine.Deregister();
                }
        }

        [TestMethod()]
        public void RemoveServerTest()
        {
            Engine.Register();
            try
            {
                Engine.RemoveServer(ServerType.InstituteAccess, "https://vpn.tuxed.net");
                Engine.RemoveServer(ServerType.SecureInternet, "https://eva-saml-idp.eduroam.nl/simplesamlphp/saml2/idp/metadata.php");
                Engine.RemoveServer(ServerType.Own, "https://vpn.tuxed.net");
            }
            finally
            {
                Engine.Deregister();
            }

            Assert.ThrowsException<Exception>(() => Engine.RemoveServer(ServerType.InstituteAccess, "https://vpn.tuxed.net"));
            Assert.ThrowsException<Exception>(() => Engine.RemoveServer(ServerType.SecureInternet, "https://eva-saml-idp.eduroam.nl/simplesamlphp/saml2/idp/metadata.php"));
            Assert.ThrowsException<Exception>(() => Engine.RemoveServer(ServerType.Own, "https://vpn.tuxed.net"));
        }

        [TestMethod()]
        public void CurrentServerTest()
        {
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
            Engine.Register();
            try
            {
                Assert.ThrowsException<Exception>(() => Engine.SetSecureInternetLocation("https://eva-saml-idp.eduroam.nl/simplesamlphp/saml2/idp/metadata.php", "de"));
            }
            finally
            {
                Engine.Deregister();
            }
        }

        [TestMethod()]
        public void DiscoveryTest()
        {
            using (var cookie = new Engine.Cookie())
            {
                Engine.Register();
                try
                {
                    eduJSON.Parser.Parse(Engine.DiscoServers(cookie));
                    eduJSON.Parser.Parse(Engine.DiscoOrganizations(cookie));
                }
                finally
                {
                    Engine.Deregister();
                }

                Assert.ThrowsException<Exception>(() => Engine.DiscoServers(cookie));
                Assert.ThrowsException<Exception>(() => Engine.DiscoOrganizations(cookie));
            }
        }

        [TestMethod()]
        public void CleanupTest()
        {
            Engine.Register();
            using (var cookie = new Engine.Cookie())
                try
                {
                    Assert.ThrowsException<Exception>(() => Engine.Cleanup(cookie));
                }
                finally
                {
                    Engine.Deregister();
                }
        }

        [TestMethod()]
        public void RenewSessionTest()
        {
            Engine.Register();
            using (var cookie = new Engine.Cookie())
                try
                {
                    Assert.ThrowsException<Exception>(() => Engine.RenewSession(cookie));
                }
                finally
                {
                    Engine.Deregister();
                }
        }

        [TestMethod()]
        public void FailoverTest()
        {
            Engine.Register();
            using (var cookie = new Engine.Cookie())
                try
                {
                    long rx = 0;
                    long tx = 0;
                    Engine.ReportTraffic += (object sender, Engine.ReportTrafficEventArgs e) =>
                    {
                        e.RxBytes = rx;
                        e.TxBytes = tx;
                    };
                    bool dropped = false;
                    Exception exception = null;
                    var t = new Thread(() =>
                    {
                        try { dropped = Engine.StartFailover(cookie, "172.217.19.100", 1450); } catch (Exception ex) { exception = ex; }
                    });
                    t.Start();
                    for (int i = 0; i < 50; i++)
                    {
                        Thread.Sleep(50);
                        rx += 100;
                        tx += 100;
                    }
                    t.Join();
                    Assert.AreEqual(null, exception);
                    Assert.AreEqual(false, dropped);
                    rx = tx = 0;
                    t = new Thread(() =>
                    {
                        try { dropped = Engine.StartFailover(cookie, "172.217.19.100", 5000); } catch (Exception ex) { exception = ex; }
                    });
                    t.Start();
                    t.Join();
                    Assert.AreEqual(null, exception);
                    Assert.AreEqual(true, dropped);
            }
            finally
            {
                Engine.Deregister();
            }
        }
    }
}

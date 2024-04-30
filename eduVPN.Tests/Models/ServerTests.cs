/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace eduVPN.Models.Tests
{
    [TestClass()]
    public class ServerTests
    {
        [TestMethod()]
        public void ServerTest()
        {
            Server srv;

            srv = new InstituteAccessServer(new Server.Json
            {
                base_url = new Uri("https://surf.eduvpn.nl/")
            });
            Assert.AreEqual(new Uri("https://surf.eduvpn.nl/").AbsoluteUri, srv.Id, "Base URI incorrect");
            Assert.AreEqual("surf.eduvpn.nl", srv.ToString(), "Display name incorrect");

            srv = new InstituteAccessServer(new Server.Json
            {
                base_url = new Uri("https://surf.eduvpn.nl/"),
                display_name = "SURF",
            });
            Assert.AreEqual(new Uri("https://surf.eduvpn.nl/").AbsoluteUri, srv.Id, "Base URI incorrect");
            Assert.AreEqual("SURF", srv.ToString(), "Display name incorrect");

            srv = new SecureInternetServer(new Server.Json
            {
                base_url = new Uri("https://surf.eduvpn.nl/"),
                country_code = "NL",
                support_contacts = new List<Uri>() { new Uri("mailto:info@surf.nl") },
            });
            Assert.AreEqual(new Uri("https://surf.eduvpn.nl/").AbsoluteUri, srv.Id, "Base URI incorrect");
            Assert.AreEqual(new Country("NL").ToString(), srv.ToString(), "Display name incorrect");
            Assert.AreEqual("NL", ((SecureInternetServer)srv).Country.Code, "Country code incorrect");

            // Test issues.
            Assert.ThrowsException<ArgumentException>(() =>
            {
                srv = new InstituteAccessServer(new Server.Json
                {
                    display_name = "SURF",
                });
            });
            Assert.ThrowsException<ArgumentException>(() =>
            {
                srv = new SecureInternetServer(new Server.Json
                {
                    display_name = "SURF",
                });
            });
        }
    }
}
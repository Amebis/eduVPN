/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace eduVPN.Models.Tests
{
    [TestClass()]
    public class InstanceInfoTests
    {
        [TestMethod()]
        public void InstanceInfoTest()
        {
            Instance inst;

            inst = new Instance();
            inst.Load(new Dictionary<string, object>
                {
                    { "base_uri", "https://surf.eduvpn.nl/" }
                });
            Assert.AreEqual(new Uri("https://surf.eduvpn.nl/"), inst.Base, "Base URI incorrect");
            Assert.AreEqual("surf.eduvpn.nl", inst.DisplayName, "Display name incorrect");
            Assert.AreEqual(null, inst.Logo, "Logo URI incorrect");

            inst = new Instance();
            inst.Load(new Dictionary<string, object>
                {
                    { "base_uri", "https://surf.eduvpn.nl/" },
                    { "display_name", "SURF" }
                });
            Assert.AreEqual(new Uri("https://surf.eduvpn.nl/"), inst.Base, "Base URI incorrect");
            Assert.AreEqual("SURF", inst.DisplayName, "Display name incorrect");
            Assert.AreEqual(null, inst.Logo, "Logo URI incorrect");

            inst = new Instance();
            inst.Load(new Dictionary<string, object>
                {
                    { "base_uri", "https://surf.eduvpn.nl/" },
                    { "display_name", "SURF" },
                    { "logo", "https://static.eduvpn.nl/img/surfnet.png" }
                });
            Assert.AreEqual(new Uri("https://surf.eduvpn.nl/"), inst.Base, "Base URI incorrect");
            Assert.AreEqual("SURF", inst.DisplayName, "Display name incorrect");
            Assert.AreEqual(new Uri("https://static.eduvpn.nl/img/surfnet.png"), inst.Logo, "Logo URI incorrect");

            // Test issues.
            try
            {
                inst = new Instance();
                inst.Load(new Dictionary<string, object>
                    {
                        { "display_name", "SURF" },
                        { "logo", "https://static.eduvpn.nl/img/surfnet.png" }
                    });
                Assert.Fail("Missing base URL tolerated");
            } catch (eduJSON.MissingParameterException) {}
        }
    }
}
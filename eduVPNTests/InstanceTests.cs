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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace eduVPN.Tests
{
    [TestClass()]
    public class InstanceTests
    {
        [TestMethod()]
        public void InstanceTest()
        {
            Instance obj;

            obj = new Instance("https://surf.eduvpn.nl/");
            Assert.AreEqual(new System.Uri("https://surf.eduvpn.nl/"), obj.BaseURI, "Base URI incorrect");
            Assert.AreEqual("surf.eduvpn.nl", obj.DisplayName, "Display name incorrect");
            Assert.AreEqual(null, obj.LogoURI, "Logo URI incorrect");

            obj = new Instance("https://surf.eduvpn.nl/", "SURF");
            Assert.AreEqual(new System.Uri("https://surf.eduvpn.nl/"), obj.BaseURI, "Base URI incorrect");
            Assert.AreEqual("SURF", obj.DisplayName, "Display name incorrect");
            Assert.AreEqual(null, obj.LogoURI, "Logo URI incorrect");

            obj = new Instance(new Dictionary<string, object>
                {
                    { "base_uri", "https://surf.eduvpn.nl/" },
                    { "display_name", "SURF" },
                    { "logo_uri", "https://static.eduvpn.nl/img/surfnet.png" }
                });
            Assert.AreEqual(new System.Uri("https://surf.eduvpn.nl/"), obj.BaseURI, "Base URI incorrect");
            Assert.AreEqual("SURF", obj.DisplayName, "Display name incorrect");
            Assert.AreEqual(new System.Uri("https://static.eduvpn.nl/img/surfnet.png"), obj.LogoURI, "Logo URI incorrect");

            // Test issues.
            try
            {
                obj = new Instance(new Dictionary<string, object>
                    {
                        { "display_name", "SURF" },
                        { "logo_uri", "https://static.eduvpn.nl/img/surfnet.png" }
                    });
                Assert.Fail("Missing base URL tolerated");
            } catch (ArgumentException) {}
        }
    }
}
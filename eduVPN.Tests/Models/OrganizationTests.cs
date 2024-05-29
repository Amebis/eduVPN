/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace eduVPN.Models.Tests
{
    [TestClass()]
    public class OrganizationTests
    {
        [TestMethod()]
        public void OrganizationTest()
        {
            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            Organization org;

            org = new Organization(new Organization.Json
            {
                org_id = "https://idp.surfnet.nl",
                secure_internet_home = new Uri("https://nl.eduvpn.org/"),
            });
            Assert.AreEqual("https://idp.surfnet.nl", org.Id, "Identifier URI incorrect");
            Assert.AreEqual(new Uri("https://nl.eduvpn.org/"), org.SecureInternetBase, "Secure internet home server base URI incorrect");
            Assert.AreEqual("https://idp.surfnet.nl", org.ToString(), "Display name incorrect");

            org = new Organization(new Organization.Json
            {
                org_id = "https://idp.surfnet.nl",
                secure_internet_home = new Uri("https://nl.eduvpn.org/"),
                display_name = new Dictionary<string, string>() {
                    { "nl", "SURFnet bv" },
                    { "en", "SURFnet bv" },
                },
                keyword_list = new Dictionary<string, string>() {
                    { "en", "SURFnet bv SURF konijn surf surfnet powered by" },
                    { "nl", "SURFnet bv SURF konijn powered by" },
                },
            });
            Assert.AreEqual("https://idp.surfnet.nl", org.Id, "Identifier URI incorrect");
            Assert.AreEqual(new Uri("https://nl.eduvpn.org/"), org.SecureInternetBase, "Secure internet home server base URI incorrect");
            Assert.AreEqual("SURFnet bv", org.ToString(), "Display name incorrect");

            // Test issues.
            Assert.ThrowsException<ArgumentException>(() =>
                new Organization(new Organization.Json
                {
                    secure_internet_home = new Uri("https://nl.eduvpn.org/"),
                }));
        }

        [TestMethod()]
        public void OrganizationLocalizationTest()
        {
            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            var list = new List<Organization>();

            var el1 = new Organization(new Organization.Json
            {
                org_id = "https://idp.surfnet.nl",
                secure_internet_home = new Uri("https://nl.eduvpn.org/"),
                display_name = new Dictionary<string, string>() {
                    { "nl", "SURFnet bv" },
                    { "en", "SURFnet bv" },
                },
                keyword_list = new Dictionary<string, string>() {
                    { "en", "SURFnet bv SURF konijn surf surfnet powered by" },
                    { "nl", "SURFnet bv SURF konijn powered by" },
                },
            });
            list.Add(el1);
            Assert.AreEqual("SURFnet bv", el1.ToString());
            Assert.IsTrue(el1.LocalizedKeywordSets["nl"].Contains("surf"));

            var el2 = new Organization(new Organization.Json
            {
                org_id = "https://www.arnes.si",
                secure_internet_home = new Uri("https://si.eduvpn.org/"),
                display_name = new Dictionary<string, string>() {
                        { "sl", "Akademska in raziskovalna mreža Slovenije" },
                        { "en", "Academic and Research Network of Slovenia" },
                    },
                keyword_list = "ARNES",
            });
            list.Add(el2);
            Assert.AreEqual("Academic and Research Network of Slovenia", el2.ToString());
            Assert.IsTrue(el2.LocalizedKeywordSets[""].Contains("ARNES"));
        }
    }
}

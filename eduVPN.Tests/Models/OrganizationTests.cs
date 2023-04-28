/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
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

            org = new Organization(new Dictionary<string, object>
                {
                    { "org_id", "https://idp.surfnet.nl" },
                    { "secure_internet_home", "https://nl.eduvpn.org/" },
                });
            Assert.AreEqual(new Uri("https://idp.surfnet.nl/"), org.Id, "Identifier URI incorrect");
            Assert.AreEqual(new Uri("https://nl.eduvpn.org/"), org.SecureInternetBase, "Secure internet home server base URI incorrect");
            Assert.AreEqual("https://idp.surfnet.nl", org.ToString(), "Display name incorrect");

            org = new Organization(new Dictionary<string, object>
                {
                    { "org_id", "https://idp.surfnet.nl" },
                    { "secure_internet_home", "https://nl.eduvpn.org/" },
                    {  "display_name", new Dictionary<string, object>() {
                        { "nl", "SURFnet bv" },
                        { "en", "SURFnet bv" },
                    }},
                    { "keyword_list", new Dictionary<string, object>() {
                        { "en", "SURFnet bv SURF konijn surf surfnet powered by" },
                        { "nl", "SURFnet bv SURF konijn powered by" },
                    }}
                });
            Assert.AreEqual(new Uri("https://idp.surfnet.nl/"), org.Id, "Identifier URI incorrect");
            Assert.AreEqual(new Uri("https://nl.eduvpn.org/"), org.SecureInternetBase, "Secure internet home server base URI incorrect");
            Assert.AreEqual("SURFnet bv", org.ToString(), "Display name incorrect");

            // Test issues.
            Assert.ThrowsException<eduJSON.MissingParameterException>(() =>
                new Organization(new Dictionary<string, object>
                {
                    { "secure_internet_home", "https://nl.eduvpn.org/" },
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
            var idx = new Dictionary<string, HashSet<Organization>>(StringComparer.InvariantCultureIgnoreCase);

            var el1 = new Organization(new Dictionary<string, object>
                {
                    { "org_id", "https://idp.surfnet.nl" },
                    { "secure_internet_home", "https://nl.eduvpn.org/" },
                    { "display_name", new Dictionary<string, object>() {
                        { "nl", "SURFnet bv" },
                        { "en", "SURFnet bv" },
                    }},
                    { "keyword_list", new Dictionary<string, object>() {
                        { "en", "SURFnet bv SURF konijn surf surfnet powered by" },
                        { "nl", "SURFnet bv SURF konijn powered by" },
                    }}
                });
            list.Add(el1);
            idx.IndexName(el1);
            idx.IndexKeywords(el1);
            Assert.AreEqual("SURFnet bv", el1.ToString());
            Assert.IsTrue(el1.LocalizedKeywordSets["nl"].Contains("surf"));

            var el2 = new Organization(new Dictionary<string, object>
                {
                    { "org_id", "https://www.arnes.si" },
                    { "secure_internet_home", "https://si.eduvpn.org/" },
                    { "display_name", new Dictionary<string, object>() {
                        { "sl", "Akademska in raziskovalna mreža Slovenije" },
                        { "en", "Academic and Research Network of Slovenia" },
                    }},
                    { "keyword_list", "ARNES" },
                });
            list.Add(el2);
            idx.IndexName(el2);
            idx.IndexKeywords(el2);
            Assert.AreEqual("Academic and Research Network of Slovenia", el2.ToString());
            Assert.IsTrue(el2.LocalizedKeywordSets[""].Contains("ARNES"));

            // Index tests
            Assert.IsTrue(idx["SURFnet"].Contains(el1));
            Assert.IsFalse(idx["SURFnet"].Contains(el2));
            Assert.IsTrue(idx["kon"].Contains(el1));
            Assert.IsFalse(idx["kon"].Contains(el2));
            Assert.IsFalse(idx["mreža"].Contains(el1));
            Assert.IsTrue(idx["mreža"].Contains(el2));
            Assert.IsFalse(idx["rne"].Contains(el1));
            Assert.IsTrue(idx["rne"].Contains(el2));
        }
    }
}

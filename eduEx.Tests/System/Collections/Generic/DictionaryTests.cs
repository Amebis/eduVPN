/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace eduVPN.System.Collections.Generic
{
    [TestClass()]
    public class DictionaryTests
    {
        [TestMethod()]
        public void DictionaryGetLocalizedTest()
        {
            CultureInfo culture;
            var key1 = new Dictionary<string, string>()
            {
                { "", "<language independent>" },
            };
            var key2 = new Dictionary<string, string>()
            {
                { "de-DE", "Sprache" },
                { "en-US", "Language" },
            };
            var key3 = new Dictionary<string, string>()
            {
                { "de-DE", "Nur Deutsch" },
            };

            // Set language preference to German (Germany).
            culture = new CultureInfo("de-DE");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            Assert.AreEqual(key1.GetLocalized(), "<language independent>");
            Assert.AreEqual(key2.GetLocalized(), "Sprache");
            Assert.AreEqual(key3.GetLocalized(), "Nur Deutsch");

            // Set language preference to Slovenian (Slovenia).
            culture = new CultureInfo("sl-SI");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            Assert.AreEqual(key1.GetLocalized(), "<language independent>");
            Assert.AreEqual(key2.GetLocalized(), "Language");
            Assert.AreEqual(key3.GetLocalized(), "Nur Deutsch");

            // Set language preference to English (U.S.).
            culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            Assert.AreEqual(key1.GetLocalized(), "<language independent>");
            Assert.AreEqual(key2.GetLocalized(), "Language");
            Assert.AreEqual(key3.GetLocalized(), "Nur Deutsch");
        }
    }
}
/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduJSON;
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
            var obj = Parser.Parse("{ \"key1\": \"<language independent>\", \"key2\": { \"de-DE\": \"Sprache\", \"en-US\": \"Language\" }, \"key3\": { \"de-DE\": \"Nur Deutsch\" } }") as Dictionary<string, object>;
            var key1 = Parser.GetDictionary<string>(obj, "key1");
            var key2 = Parser.GetDictionary<string>(obj, "key2");
            var key3 = Parser.GetDictionary<string>(obj, "key3");

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
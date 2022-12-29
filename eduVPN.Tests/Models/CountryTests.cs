/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Threading;

namespace eduVPN.Models.Tests
{
    [TestClass()]
    public class CountryTests
    {
        [TestMethod()]
        public void CountryTest()
        {
            foreach (var c in Country.Countries)
            {
                var country = new Country(c.Key);
                country.ToString();
            }

            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            Assert.AreEqual("Antarctica", new Country("AQ").ToString());
            Assert.AreEqual("Netherlands", new Country("NL").ToString());
        }
    }
}
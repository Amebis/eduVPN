/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace eduVPN.Models.Tests
{
    [TestClass()]

    public class OrganizationsTests
    {
        [TestMethod()]
        public void OrganizationsTest()
        {
            byte[] organizationListJson = System.Text.Encoding.UTF8.GetBytes(@"{
  ""organization_list"": [
    {
      ""display_name"": {
        ""nl"": ""SURFnet bv"",
        ""en"": ""SURFnet bv""
      },
      ""org_id"": ""https://idp.surfnet.nl"",
      ""secure_internet_home"": ""https://nl.eduvpn.org/"",
      ""keyword_list"": {
        ""en"": ""SURFnet bv SURF konijn surf surfnet powered by"",
        ""nl"": ""SURFnet bv SURF konijn powered by""
      }
    }
  ]
}");
            var dict = new OrganizationDictionary(Utf8Json.JsonSerializer.Deserialize<OrganizationDictionary.Json>(organizationListJson));

            var org = dict["https://idp.surfnet.nl"];
            Assert.AreEqual(new Uri("https://nl.eduvpn.org/"), org.SecureInternetBase, "Secure internet base incorrect");
        }
    }
}

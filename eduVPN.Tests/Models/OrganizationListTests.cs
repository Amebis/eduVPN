﻿/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using eduVPN.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace eduVPN.Models.Tests
{
    [TestClass()]

    public class OrganizationsTests
    {
        [TestMethod()]
        public void OrganizationsTest()
        {
            const string organizationListJson = @"{
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
}";
            var dict = new OrganizationDictionary();
            dict.LoadJSON(organizationListJson);

            var org = dict["https://idp.surfnet.nl"];
            Assert.AreEqual(new Uri("https://nl.eduvpn.org/"), org.SecureInternetBase, "Secure internet base incorrect");
        }

        [TestMethod()]
        public void OrganizationsNetworkTest()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            var xmlReader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(@"<ResourceRef Uri=""https://disco.eduvpn.org/v2/organization_list.json"">
						<MinisignPublicKeyDictionary Key=""PublicKeys"">
							<PublicKey>RWRtBSX1alxyGX+Xn3LuZnWUT0w//B6EmTJvgaAxBMYzlQeI+jdrO6KF</PublicKey>
							<PublicKey>RWQKqtqvd0R7rUDp0rWzbtYPA3towPWcLDCl7eY9pBMMI/ohCmrS0WiM</PublicKey>
						</MinisignPublicKeyDictionary>
					</ResourceRef>")));
            while (xmlReader.ReadState == ReadState.Initial)
                xmlReader.Read();
            var source = new ResourceRef();
            source.ReadXml(xmlReader);

            // Load list of organizations.
            var organizationListJson = Response.Get(source);
            var dict = new OrganizationDictionary();
            dict.LoadJSON(organizationListJson.Value);

            // Re-load list of organizations.
            Response.Get(
                res: source,
                previous: organizationListJson);
        }
    }
}

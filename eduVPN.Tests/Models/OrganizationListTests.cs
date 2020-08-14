/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
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
            // .NET 3.5 allows Schannel to use SSL 3 and TLS 1.0 by default. Instead of hacking user computer's registry, extend it in runtime.
            // System.Net.SecurityProtocolType lacks appropriate constants prior to .NET 4.5.
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)0x0C00;

            var xmlReader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(@"<ResourceRef Uri=""https://disco.eduvpn.org/v2/organization_list.json"">
						<MinisignPublicKeyDictionary Key=""PublicKeys"">
							<PublicKey>RWRtBSX1alxyGX+Xn3LuZnWUT0w//B6EmTJvgaAxBMYzlQeI+jdrO6KF</PublicKey>
							<PublicKey>RWQ68Y5/b8DED0TJ41B1LE7yAvkmavZWjDwCBUuC+Z2pP9HaSawzpEDA</PublicKey>
							<PublicKey>RWQKqtqvd0R7rUDp0rWzbtYPA3towPWcLDCl7eY9pBMMI/ohCmrS0WiM</PublicKey>
						</MinisignPublicKeyDictionary>
					</ResourceRef>")));
            while (xmlReader.ReadState == ReadState.Initial)
                xmlReader.Read();
            var source = new ResourceRef();
            source.ReadXml(xmlReader);

            // Load list of organizations.
            var organizationListJson = Xml.Response.Get(source);
            var dict = new OrganizationDictionary();
            dict.LoadJSON(organizationListJson.Value);

            // Re-load list of organizations.
            Xml.Response.Get(
                res: source,
                previous: organizationListJson);
        }
    }
}
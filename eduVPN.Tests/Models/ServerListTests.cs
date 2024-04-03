/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace eduVPN.Models.Tests
{
    [TestClass()]
    public class ServersTests
    {
        [TestMethod()]
        public void ServersTest()
        {
            const string server_list_list_json = @"{
  ""server_list"": [
    {
                ""server_type"": ""institute_access"",
      ""base_url"": ""https://sunset.nuonet.fr/"",
      ""display_name"": ""CNOUS"",
      ""support_contact"": [
        ""mailto:support-technique-nuo@listes.nuonet.fr""
      ]
    },
    {
                ""server_type"": ""secure_internet"",
      ""base_url"": ""https://eduvpn.rash.al/"",
      ""country_code"": ""AL"",
      ""support_contact"": [
        ""mailto:helpdesk@rash.al""
      ]
    }
  ]
}";
            var server_list_list_ia = new ServerDictionary();
            server_list_list_ia.LoadJSON(server_list_list_json);

            Assert.IsInstanceOfType(server_list_list_ia[new Uri("https://sunset.nuonet.fr/")], typeof(InstituteAccessServer));
            Assert.IsInstanceOfType(server_list_list_ia[new Uri("https://eduvpn.rash.al/")], typeof(SecureInternetServer));
        }
    }
}

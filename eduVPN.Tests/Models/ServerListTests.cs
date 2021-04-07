/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using eduVPN.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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

        [TestMethod()]
        public void ServersNetworkTest()
        {
            // .NET 3.5 allows Schannel to use SSL 3 and TLS 1.0 by default. Instead of hacking user computer's registry, extend it in runtime.
            // System.Net.SecurityProtocolType lacks appropriate constants prior to .NET 4.5.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | (SecurityProtocolType)0x3000;

            var xmlReader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(@"<ResourceRef Uri=""https://disco.eduvpn.org/v2/server_list.json"">
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

            // Load list of servers.
            var server_list_list_json = Xml.Response.Get(source);
            var server_list_list_ia = new ServerDictionary();
            server_list_list_ia.LoadJSON(server_list_list_json.Value);

            // Load all servers APIs.
            Parallel.ForEach(server_list_list_ia.Values, srv =>
            {
                var uriBuilder = new UriBuilder(srv.Base);
                uriBuilder.Path += "info.json";
                try
                {
                    new Models.ServerEndpoints().LoadJSON(Xml.Response.Get(uriBuilder.Uri).Value);
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerException is WebException ex_web &&
                        (ex_web.Status == WebExceptionStatus.ConnectFailure || // connection refused
                        ex_web.Status == WebExceptionStatus.TrustFailure || // expired or invalid server certificate
                        ex_web.Status == WebExceptionStatus.SecureChannelFailure || // TLS failure
                        ex_web.Status == WebExceptionStatus.Timeout)) // server down
                    {
                        // Ignore connection failure WebException(s), as some servers are not publicly available or have other issues.
                    }
                    else
                        throw;
                }
            });

            // Re-load list of servers.
            server_list_list_json = Xml.Response.Get(
                res: source,
                previous: server_list_list_json);
        }
    }
}
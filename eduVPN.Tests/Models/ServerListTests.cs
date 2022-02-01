/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
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
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            var xmlReader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(@"<ResourceRef Uri=""https://disco.eduvpn.org/v2/server_list.json"">
						<MinisignPublicKeyDictionary Key=""PublicKeys"">
							<PublicKey>RWRtBSX1alxyGX+Xn3LuZnWUT0w//B6EmTJvgaAxBMYzlQeI+jdrO6KF</PublicKey>
							<PublicKey>RWQKqtqvd0R7rUDp0rWzbtYPA3towPWcLDCl7eY9pBMMI/ohCmrS0WiM</PublicKey>
						</MinisignPublicKeyDictionary>
					</ResourceRef>")));
            while (xmlReader.ReadState == ReadState.Initial)
                xmlReader.Read();
            var source = new ResourceRef();
            source.ReadXml(xmlReader);

            // Load list of servers.
            var server_list_list_json = Response.Get(source);
            var server_list_list_ia = new ServerDictionary();
            server_list_list_ia.LoadJSON(server_list_list_json.Value);

            // Load all servers APIs.
            Parallel.ForEach(server_list_list_ia.Values, srv =>
            {
                var uriBuilder = new UriBuilder(srv.Base);
                uriBuilder.Path += "info.json";
                try
                {
                    new ServerEndpoints().LoadJSON(Response.Get(uriBuilder.Uri).Value);
                }
                catch (UnsupportedServerAPIException)
                {
                    // Ignore non-APIv3 servers.
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerException is WebException ex_web &&
                        (ex_web.Status == WebExceptionStatus.NameResolutionFailure || // DNS resolving failure
                        ex_web.Status == WebExceptionStatus.ConnectFailure || // connection refused
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
            server_list_list_json = Response.Get(
                res: source,
                previous: server_list_list_json);
        }
    }
}

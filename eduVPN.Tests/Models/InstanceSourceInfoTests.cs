/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace eduVPN.Models.Tests
{
    [TestClass()]
    public class InstanceSourceInfoTests
    {
        [TestMethod()]
        public void InstanceSourceInfoTest()
        {
            // .NET 3.5 allows Schannel to use SSL 3 and TLS 1.0 by default. Instead of hacking user computer's registry, extend it in runtime.
            // System.Net.SecurityProtocolType lacks appropriate constants prior to .NET 4.5.
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)0x0C00;

            var pub_key = Convert.FromBase64String("E5On0JTtyUVZmcWd+I/FXRm32nSq8R2ioyW7dcu/U88=");
            Parallel.ForEach(new List<KeyValuePair<Uri, byte[]>>()
                {
                    new KeyValuePair<Uri, byte[]>(new Uri("https://static.eduvpn.nl/disco/institute_access.json"), pub_key),
                    new KeyValuePair<Uri, byte[]>(new Uri("https://static.eduvpn.nl/disco/secure_internet.json"), pub_key),
                }, source =>
                {
                    // Load list of instances.
                    var instance_source_json = Xml.Response.Get(
                        uri: source.Key,
                        pub_key: source.Value);
                    var instance_source_ia = new InstanceSource();
                    instance_source_ia.LoadJSON(instance_source_json.Value);

                    // Load all instances APIs.
                    Parallel.ForEach(instance_source_ia.InstanceList, i =>
                    {
                        var uri_builder = new UriBuilder(i.Base);
                        uri_builder.Path += "info.json";
                        new Models.InstanceEndpoints().LoadJSON(Xml.Response.Get(uri_builder.Uri).Value);
                    });

                    // Re-load list of instances.
                    instance_source_json = Xml.Response.Get(
                        uri: source.Key,
                        pub_key: source.Value,
                        previous: instance_source_json);
                });
        }

#if PLATFORM_AnyCPU
        private static bool is_resolver_active = eduBase.MultiplatformDllLoader.Enable = true;
#endif
    }
}
/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace eduVPN.Tests
{
    [TestClass()]
    public class InstanceListTests
    {
        [TestMethod()]
        public void InstanceListTest()
        {
            // .NET 3.5 allows Schannel to use SSL 3 and TLS 1.0 by default. Instead of hacking user computer's registry, extend it in runtime.
            // System.Net.SecurityProtocolType lacks appropriate constants prior to .NET 4.5.
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)0x0C00;

            // Get instance list.
            var instance_list_json = JSONContents.Get(
                new Uri("https://static.eduvpn.nl/instances.json"),
                Convert.FromBase64String("E5On0JTtyUVZmcWd+I/FXRm32nSq8R2ioyW7dcu/U88="));

            // Re-get instance list.
            instance_list_json = JSONContents.Get(
                new Uri("https://static.eduvpn.nl/instances.json"),
                Convert.FromBase64String("E5On0JTtyUVZmcWd+I/FXRm32nSq8R2ioyW7dcu/U88="),
                default(CancellationToken),
                instance_list_json);

            // Load list of instances.
            var instance_list = new InstanceList();
            instance_list.Load(instance_list_json.Value);

            // Load all instance API(s) in parallel.
            Task.WhenAll(instance_list.Select(async i => {
                var uri_builder = new UriBuilder(i.Base);
                uri_builder.Path += "info.json";
                new API().Load((await JSONContents.GetAsync(uri_builder.Uri)).Value);
            })).Wait();
        }

#if PLATFORM_AnyCPU
        private static bool is_resolver_active = eduBase.MultiplatformDllLoader.Enable = true;
#endif
    }
}
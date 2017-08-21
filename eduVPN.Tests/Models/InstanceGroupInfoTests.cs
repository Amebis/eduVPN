/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace eduVPN.Models.Tests
{
    [TestClass()]
    public class InstanceGroupInfoTests
    {
        [TestMethod()]
        public void InstanceGroupInfoTest()
        {
            // .NET 3.5 allows Schannel to use SSL 3 and TLS 1.0 by default. Instead of hacking user computer's registry, extend it in runtime.
            // System.Net.SecurityProtocolType lacks appropriate constants prior to .NET 4.5.
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)0x0C00;

            // Spawn instance group get (Institute Access).
            var instance_group_ia_json_task = JSON.Response.GetAsync(
                uri: new Uri("https://static.eduvpn.nl/disco/institute_access.json"),
                pub_key: Convert.FromBase64String("E5On0JTtyUVZmcWd+I/FXRm32nSq8R2ioyW7dcu/U88="));

            // Spawn instance group get (Secure Internet).
            var instance_group_si_json_task = JSON.Response.GetAsync(
                uri: new Uri("https://static.eduvpn.nl/disco/secure_internet.json"),
                pub_key: Convert.FromBase64String("E5On0JTtyUVZmcWd+I/FXRm32nSq8R2ioyW7dcu/U88="));

            // Load list of institute access instances.
            var instance_group_ia = new InstanceGroupInfo();
            try
            {
                instance_group_ia_json_task.Wait();
                instance_group_ia.LoadJSON(instance_group_ia_json_task.Result.Value);
            }
            catch (AggregateException ex) { throw ex.InnerException; }

            // Re-spawn instance group get.
            instance_group_ia_json_task = JSON.Response.GetAsync(
                uri: new Uri("https://static.eduvpn.nl/disco/institute_access.json"),
                pub_key: Convert.FromBase64String("E5On0JTtyUVZmcWd+I/FXRm32nSq8R2ioyW7dcu/U88="),
                previous: instance_group_ia_json_task.Result);

            // Load all institute access instances API in parallel.
            Task.WhenAll(instance_group_ia.Select(async i => {
                var uri_builder = new UriBuilder(i.Base);
                uri_builder.Path += "info.json";
                new Models.InstanceEndpoints().LoadJSON((await JSON.Response.GetAsync(uri_builder.Uri)).Value);
            })).Wait();

            // Load all secure internet instances API in parallel.
            var instance_group_si = new InstanceGroupInfo();
            try
            {
                instance_group_si_json_task.Wait();
                instance_group_si.LoadJSON(instance_group_ia_json_task.Result.Value);
            }
            catch (AggregateException ex) { throw ex.InnerException; }
            Task.WhenAll(instance_group_si.Select(async i => {
                var uri_builder = new UriBuilder(i.Base);
                uri_builder.Path += "info.json";
                new Models.InstanceEndpoints().LoadJSON((await JSON.Response.GetAsync(uri_builder.Uri)).Value);
            })).Wait();

            // Re-load list of institute access instances.
            try
            {
                instance_group_ia_json_task.Wait();
                instance_group_ia.LoadJSON(instance_group_ia_json_task.Result.Value);
            }
            catch (AggregateException ex) { throw ex.InnerException; }
        }

#if PLATFORM_AnyCPU
        private static bool is_resolver_active = eduBase.MultiplatformDllLoader.Enable = true;
#endif
    }
}
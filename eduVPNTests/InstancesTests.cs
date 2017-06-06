/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;

namespace eduVPN.Tests
{
    [TestClass()]
    public class InstancesTests
    {
        [TestMethod()]
        public void InstancesTest()
        {
            Instances insts;

            // .NET 3.5 allows Schannel to use SSL 3 and TLS 1.0 by default. Instead of hacking user computer's registry, extend it in runtime.
            // System.Net.SecurityProtocolType lacks appropriate constants prior to .NET 4.5.
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)0x0C00;

            insts = new Instances(new Uri("https://static.eduvpn.nl/instances.json"));

        }
    }
}
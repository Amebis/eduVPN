/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace eduVPN.Xml.Tests
{
    [TestClass()]
    public class MinisignPublicKeyDictionaryTests
    {
        [TestMethod()]
        public void MinisignPublicKeyDictionaryTest()
        {
            var xmlReader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(@"<MinisignPublicKeyDictionary>
    <PublicKey>RWRPrtnepBgoU86pKtSnHJXBtmtJjv6T5wN2Q+P7vPgHPdr3v8kGme13</PublicKey>
    <PublicKey>RWTbIHtCWd57+tcyjPSn30I7xhPGow35NR7wBzj3qDm13TE6YFk2L2M8</PublicKey>
    <PublicKey SupportedAlgorithms=""2"">RWQHk3PWKr6pfbb7MSTJrhHrPgz3/BYk8uvwFoScHK5LYZhC2oNXnW16</PublicKey>
</MinisignPublicKeyDictionary>")));

            var dict = new MinisignPublicKeyDictionary();
            dict.ReadXml(xmlReader);

            var k1 = new MinisignPublicKey(@"RWTbIHtCWd57+tcyjPSn30I7xhPGow35NR7wBzj3qDm13TE6YFk2L2M8");
            k1 = dict.FirstOrDefault(k => k.Equals(k1));
            Assert.IsNotNull(k1, "Key not found");
            Assert.AreEqual(k1.SupportedAlgorithms, MinisignPublicKey.AlgorithmMask.All);

            var k2 = new MinisignPublicKey(@"RWQHk3PWKr6pfbb7MSTJrhHrPgz3/BYk8uvwFoScHK5LYZhC2oNXnW16");
            k2 = dict.FirstOrDefault(k => k.Equals(k2));
            Assert.IsNotNull(k2, "Key not found");
            Assert.AreEqual(k2.SupportedAlgorithms, MinisignPublicKey.AlgorithmMask.Hashed);

            var k3 = new MinisignPublicKey(@"RWTbIHtCWd57+tcyjPSn30I7yhPGow35NR7wBzj3qDm13TE6YFk2L2M8");
            k3 = dict.FirstOrDefault(k => k.Equals(k3));
            Assert.IsNull(k3, "Non-existing key");
        }
    }
}

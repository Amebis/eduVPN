/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
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

            Assert.IsTrue(dict.ContainsKey(0xfa7bde59427b20db), "Key not found");
            Assert.IsFalse(dict.ContainsKey(0xdb207b4259de7bfa), "Non-existing key");
            CollectionAssert.AreEqual(dict[0xfa7bde59427b20db].Value, new byte[] {
                0xd7, 0x32, 0x8c, 0xf4, 0xa7, 0xdf, 0x42, 0x3b,
                0xc6, 0x13, 0xc6, 0xa3, 0x0d, 0xf9, 0x35, 0x1e,
                0xf0, 0x07, 0x38, 0xf7, 0xa8, 0x39, 0xb5, 0xdd,
                0x31, 0x3a, 0x60, 0x59, 0x36, 0x2f, 0x63, 0x3c }, "Wrong key")
            ;
            Assert.AreEqual(dict[0xfa7bde59427b20db].SupportedAlgorithms, MinisignPublicKey.AlgorithmMask.All);
            Assert.AreEqual(dict[0x7da9be2ad6739307].SupportedAlgorithms, MinisignPublicKey.AlgorithmMask.Hashed);
        }
    }
}

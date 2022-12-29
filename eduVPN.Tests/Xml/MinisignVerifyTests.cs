/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Security;
using System.Text;
using System.Xml;

namespace eduVPN.Xml.Tests
{
    [TestClass]
    public class MinisignVerifyTests
    {
        [TestMethod]
        public void MinisignVerifyTest()
        {
            var xmlReader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(@"<ResourceRef Uri=""../../Setup/eduVPN.windows.json"">
						<MinisignPublicKeyDictionary Key=""PublicKeys"">
							<PublicKey SupportedAlgorithms=""2"">RWRPrtnepBgoU86pKtSnHJXBtmtJjv6T5wN2Q+P7vPgHPdr3v8kGme13</PublicKey>
						</MinisignPublicKeyDictionary>
					</ResourceRef>")));
            while (xmlReader.ReadState == ReadState.Initial)
                xmlReader.Read();
            var source = new ResourceRef();
            source.ReadXml(xmlReader);
            Response.Get(source);

            xmlReader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(@"<ResourceRef Uri=""../../Setup/eduVPN.windows.json"">
						<MinisignPublicKeyDictionary Key=""PublicKeys"">
							<PublicKey SupportedAlgorithms=""1"">RWRPrtnepBgoU86pKtSnHJXBtmtJjv6T5wN2Q+P7vPgHPdr3v8kGme13</PublicKey>
						</MinisignPublicKeyDictionary>
					</ResourceRef>")));
            while (xmlReader.ReadState == ReadState.Initial)
                xmlReader.Read();
            source = new ResourceRef();
            source.ReadXml(xmlReader);
            Assert.ThrowsException<ArgumentException>(() => Response.Get(source));

            xmlReader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(@"<ResourceRef Uri=""../../Setup/eduVPN.windows.json"">
						<MinisignPublicKeyDictionary Key=""PublicKeys"">
							<PublicKey>RWRtBSX1alxyGX+Xn3LuZnWUT0w//B6EmTJvgaAxBMYzlQeI+jdrO6KF</PublicKey>
						</MinisignPublicKeyDictionary>
					</ResourceRef>")));
            while (xmlReader.ReadState == ReadState.Initial)
                xmlReader.Read();
            source = new ResourceRef();
            source.ReadXml(xmlReader);
            Assert.ThrowsException<SecurityException>(() => Response.Get(source));
        }
    }
}

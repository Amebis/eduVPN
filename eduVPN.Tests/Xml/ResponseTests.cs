/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Xml;

namespace eduVPN.Xml.Tests
{
    [TestClass]
    public class ResponseTests
    {
        [TestMethod]
        public void ResponseTest()
        {
            var xmlReader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(@"<ResourceRef Uri=""http://besana.amebis.si/""></ResourceRef>")));
            while (xmlReader.ReadState == ReadState.Initial)
                xmlReader.Read();
            var source = new ResourceRef();
            source.ReadXml(xmlReader);
            Response.Get(
                res: source,
                responseType: "*/*");

            xmlReader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(@"<ResourceRef Uri=""http://govorec.amebis.si/""></ResourceRef>")));
            while (xmlReader.ReadState == ReadState.Initial)
                xmlReader.Read();
            source = new ResourceRef();
            source.ReadXml(xmlReader);
            Assert.ThrowsException<HttpRedirectToUnsafeUriException>(() =>
                Response.Get(
                    res: source,
                    responseType: "*/*"));
        }
    }
}

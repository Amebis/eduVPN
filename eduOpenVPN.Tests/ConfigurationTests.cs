/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace eduOpenVPN.Tests
{
    [TestClass()]
    public class ConfigurationTests
    {
        [TestMethod()]
        public void EscapeParamValueTest()
        {
            Assert.AreEqual("0", Configuration.EscapeParamValue("0"));
            Assert.AreEqual("1", Configuration.EscapeParamValue("1"));
            Assert.AreEqual("123", Configuration.EscapeParamValue("123"));
            Assert.AreEqual("string", Configuration.EscapeParamValue("string"));
            Assert.AreEqual("ca.pem", Configuration.EscapeParamValue("ca.pem"));

            Assert.AreEqual(@"""C:\\Program Files\\OpenVPN\\config""", Configuration.EscapeParamValue(@"C:\Program Files\OpenVPN\config"));
            Assert.AreEqual(@"""THUMB:00 11 22 33 44 55 66 77 88 99 aa bb cc dd ee ff""", Configuration.EscapeParamValue(@"THUMB:00 11 22 33 44 55 66 77 88 99 aa bb cc dd ee ff"));
        }

        [TestMethod()]
        public void ParseParamsTest()
        {
            CollectionAssert.AreEqual(new List<string>(), Configuration.ParseParams(""));
            CollectionAssert.AreEqual(new List<string>(), Configuration.ParseParams("# Comment 1"));
            CollectionAssert.AreEqual(new List<string>(), Configuration.ParseParams("; Comment 2"));
            CollectionAssert.AreEqual(new List<string>() { "param" }, Configuration.ParseParams("param"));
            CollectionAssert.AreEqual(new List<string>() { "param1", "param 2", "param 3", "param 4", @"C:\test.txt" }, Configuration.ParseParams("  param1 param\\ 2 \"param 3\" 'param 4' C:\\\\test.txt   "));

            Assert.ThrowsException<ArgumentException>(() => Configuration.ParseParams(@"C:\certificate.pem"));
            Assert.ThrowsException<ArgumentException>(() => Configuration.ParseParams("\"open param"));
            Assert.ThrowsException<ArgumentException>(() => Configuration.ParseParams("'open param"));
        }
    }
}
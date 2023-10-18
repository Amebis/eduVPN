/*
    eduJSON - Lightweight JSON Parser for eduVPN (and beyond)

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace eduJSON.Tests
{
    [TestClass()]
    public class ParserTests
    {
        [TestMethod()]
        public void ParseBooleanTest()
        {
            // Test basic data types.
            Assert.AreEqual(true, Parser.Parse("// Test 1\n  True /* Trailing comment */"), "Boolean \"true\" not recognized");
            Assert.AreEqual(false, Parser.Parse("   falSe\r\n// Trailing comment"), "Boolean \"false\" not recognized");
        }

        [TestMethod()]
        public void ParseNullTest()
        {
            Assert.AreEqual(null, Parser.Parse("NULL"), "\"null\" not recognized");
        }

        [TestMethod()]
        public void ParseIntegerTest()
        {
            Assert.AreEqual(1234L, Parser.Parse(" 1234 "), "Integer not recognized");
            Assert.AreEqual(1234L, Parser.Parse(" +1234 "), "Integer with an explicit \"+\" sign not recognized (JSON EX)");
            Assert.AreEqual(-1234L, Parser.Parse(" -1234 "), "Negative integer not recognized");
        }

        [TestMethod()]
        public void ParseFloatTest()
        {
            Assert.AreEqual(1.0870e-3, (double)Parser.Parse(" +1.0870e-3 "), 1e-10, "Float not recognized");
            Assert.AreEqual(-2e+31, (double)Parser.Parse(" -2e+31 "), 1e-10, "Negative float not recognized");
        }

        [TestMethod()]
        public void ParseStringTest()
        {
            Assert.AreEqual("This is a test.", Parser.Parse(" \"This is a test.\" "), "Quoted string not recognized");
            Assert.AreEqual("  \"/\\\b\f\n\r\t\u263a\\x  ", Parser.Parse(" \"  \\\"\\/\\\\\\b\\f\\n\\r\\t\\u263A\\x  \" "), "JSON string escape sequences not recognized");
        }

        [TestMethod()]
        public void ParseStructTest()
        {
            // CollectionAssert.AreEqual() does not work for sub-collections; Check manually.
            var obj = Parser.Parse("{ \"key1\" : true , \"key2\" : [ 1, 2, 3, 4, 5], \"key3\" : { \"k1\": \"test1\", k2:\"test2\"}}");
            var objDict = (Dictionary<string, object>)obj;
            Assert.AreEqual(3, objDict.Count, "Incorrect number of child elements");
            Assert.AreEqual(true, objDict["key1"], "Child element mismatch");
            CollectionAssert.AreEqual(new List<object>
                {
                    1L,
                    2L,
                    3L,
                    4L,
                    5L
                }, (List<object>)objDict["key2"], "Child element mismatch");
            CollectionAssert.AreEqual(new Dictionary<string, object>
                {
                    { "k1", "test1" },
                    { "k2", "test2" }
                }, (Dictionary<string, object>)objDict["key3"], "Child element mismatch");

            Assert.ThrowsException<MissingClosingParenthesisException>(() => Parser.Parse("[1, 2"));
            Assert.ThrowsException<MissingSeparatorOrClosingParenthesisException>(() => Parser.Parse("[1 2]"));
            Assert.ThrowsException<MissingClosingParenthesisException>(() => Parser.Parse("{ \"k1\": 1, \"k2\": 2"));
            Assert.ThrowsException<MissingSeparatorOrClosingParenthesisException>(() => Parser.Parse("{ \"k1\": 1 \"k2\": 2 }"));
            Assert.ThrowsException<MissingSeparatorException>(() => Parser.Parse("{ \"key\"  \"value\" }"));
            Assert.ThrowsException<InvalidIdentifier>(() => Parser.Parse("{ \"k1\": 1, $$$: 2 }"));
            Assert.ThrowsException<DuplicateElementException>(() => Parser.Parse("{ \"k1\": 1, \"k1\": 2 }"));
        }

        [TestMethod()]
        public void ParseIssuesTest()
        {
            Assert.ThrowsException<TrailingDataException>(() => Parser.Parse("   false\r\nTrailing data"));
            Assert.ThrowsException<UnknownValueException>(() => Parser.Parse("abc"));
        }

        [TestMethod()]
        public void ParseGetValueTest()
        {
            var obj = Parser.Parse("{ \"k_string\": \"abc\", \"k_bool\": true, \"k_int\": 123, \"k_array\": [1, 2, 3], \"k_dict\": {} }") as Dictionary<string, object>;

            // Function result variant
            Assert.AreEqual(Parser.GetValue<string>(obj, "k_string"), "abc");
            Assert.AreEqual(Parser.GetValue<bool>(obj, "k_bool"), true);
            Assert.AreEqual(Parser.GetValue<long>(obj, "k_int"), 123);
            CollectionAssert.AreEqual(Parser.GetValue<List<object>>(obj, "k_array"), new List<object>() { 1L, 2L, 3L });
            CollectionAssert.AreEqual(Parser.GetValue<Dictionary<string, object>>(obj, "k_dict"), new Dictionary<string, object>());

            Assert.ThrowsException<MissingParameterException>(() => Parser.GetValue<string>(obj, "foobar"));
            Assert.ThrowsException<InvalidParameterTypeException>(() => Parser.GetValue<long>(obj, "k_string"));

            // Variable reference variant
            Assert.IsTrue(Parser.GetValue(obj, "k_string", out string val_string) && val_string == "abc");
            Assert.IsTrue(Parser.GetValue(obj, "k_bool", out bool val_bool) && val_bool == true);
            Assert.IsTrue(Parser.GetValue(obj, "k_int", out long val_int) && val_int == 123);
            Assert.IsTrue(Parser.GetValue(obj, "k_array", out List<object> val_array)/* && val_array.Equals(new List<object>() { 1, 2, 3 })*/);
            Assert.IsTrue(Parser.GetValue(obj, "k_dict", out Dictionary<string, object> val_dict)/* && val_dict.Equals(new Dictionary<string, object>())*/);

            Assert.IsFalse(Parser.GetValue(obj, "foobar", out val_string));

            Assert.ThrowsException<InvalidParameterTypeException>(() => Parser.GetValue(obj, "k_string", out val_int));
        }

        [TestMethod()]
        public void ParseGetDictionaryTest()
        {
            var obj = Parser.Parse("{ \"key1\": \"<language independent>\", \"key2\": { \"de-DE\": \"Sprache\", \"en-US\": \"Language\" }, \"key3\": { \"de-DE\": \"Nur Deutsch\" } }") as Dictionary<string, object>;

            {
                var aaa = new Dictionary<string, string>();
                Assert.IsFalse(Parser.GetDictionary(obj, "aaa", aaa));
                var key1_int = new Dictionary<string, long>();
                Assert.ThrowsException<InvalidParameterTypeException>(() => Parser.GetDictionary(obj, "key1", key1_int));
                var key1 = new Dictionary<string, string>();
                Assert.IsTrue(Parser.GetDictionary(obj, "key1", key1));
                Assert.IsTrue(key1[""] == "<language independent>");
                var key2 = new Dictionary<string, string>();
                Assert.IsTrue(Parser.GetDictionary(obj, "key2", key2));
                Assert.IsTrue(key2["en-US"] == "Language");
            }

            {
                Assert.ThrowsException<MissingParameterException>(() => Parser.GetDictionary<string>(obj, "aaa"));
                Assert.ThrowsException<InvalidParameterTypeException>(() => Parser.GetDictionary<long>(obj, "key1"));
                var key1 = Parser.GetDictionary<string>(obj, "key1");
                Assert.IsTrue(key1[""] == "<language independent>");
                var key2 = Parser.GetDictionary<string>(obj, "key2");
                Assert.IsTrue(key2["en-US"] == "Language");
            }
        }
    }
}

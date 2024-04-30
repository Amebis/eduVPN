/*
    eduOAuth - OAuth 2.0 Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace eduOAuth.Tests
{
    [TestClass()]
    public class AuthorizationGrantTests
    {
        [TestMethod()]
        public void AuthorizationGrantTest()
        {
            var ag = new AuthorizationGrant(
                new Uri("https://test.eduvpn.org/?param=1"),
                new Uri("org.eduvpn.app:/api/callback"),
                "org.eduvpn.app",
                new HashSet<string>() { "scope1", "scope2" });

            var uriBuilder = new UriBuilder(ag.AuthorizationUri);
            Assert.AreEqual("https", uriBuilder.Scheme);
            Assert.AreEqual("test.eduvpn.org", uriBuilder.Host);
            Assert.AreEqual("/", uriBuilder.Path);

            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            Assert.AreEqual("1", query["param"]);
            Assert.AreEqual("code", query["response_type"]);
            Assert.AreEqual("org.eduvpn.app", query["client_id"]);
            Assert.AreEqual("org.eduvpn.app:/api/callback", query["redirect_uri"]);
            CollectionAssert.AreEqual(new List<string>() { "scope1", "scope2" }, query["scope"].Split(null));
            Assert.IsTrue(AuthorizationGrant.Base64UriDecodeNoPadding(query["state"]).Length > 0);
            Assert.AreEqual("S256", query["code_challenge_method"]);
            Assert.IsTrue(AuthorizationGrant.Base64UriDecodeNoPadding(query["code_challenge"]).Length > 0);

            var request = new Mock<HttpWebRequest>();
            request.Setup(obj => obj.RequestUri).Returns(new Uri("https://demo.eduvpn.nl/portal/oauth.php/token"));
            request.SetupSet(obj => obj.Method = "POST");
            request.SetupProperty(obj => obj.Credentials);
            request.SetupProperty(obj => obj.PreAuthenticate, false);
            request.SetupSet(obj => obj.ContentType = "application/x-www-form-urlencoded");
            request.SetupProperty(obj => obj.ContentLength);
            var request_buffer = new byte[1048576];
            request.Setup(obj => obj.GetRequestStream()).Returns(new MemoryStream(request_buffer, true));
            var response = new Mock<HttpWebResponse>();
            response.Setup(obj => obj.GetResponseStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes(Global.AccessTokenJSON)));
            response.SetupGet(obj => obj.StatusCode).Returns(HttpStatusCode.OK);
            request.Setup(obj => obj.GetResponse()).Returns(response.Object);

            AccessToken
                token1 = new BearerToken(Global.AccessTokenObj, DateTimeOffset.Now),
                token2 = ag.ProcessResponse(new NameValueCollection() { { "state", query["state"] }, { "code", "1234567890" } }, request.Object, new NetworkCredential("", "password").SecurePassword);
            var request_param = HttpUtility.ParseQueryString(Encoding.ASCII.GetString(request_buffer, 0, (int)request.Object.ContentLength));
            Assert.AreEqual("authorization_code", request_param["grant_type"]);
            Assert.IsNotNull(request_param["code"]);
            Assert.AreEqual(ag.RedirectEndpoint, new Uri(request_param["redirect_uri"]));
            Assert.AreEqual(ag.ClientId, request_param["client_id"]);
            Assert.IsNotNull(request_param["code_verifier"]);
            Assert.AreEqual(token1, token2);
            Assert.IsTrue((token1.Authorized - token2.Authorized).TotalSeconds < 60);
            Assert.IsTrue((token1.Expires - token2.Expires).TotalSeconds < 60);
            Assert.IsTrue(token2.Scope != null);
            Assert.IsTrue(token1.Scope.SetEquals(token2.Scope));

            Assert.ThrowsException<ArgumentException>(() => ag.ProcessResponse(new NameValueCollection() { { "code", "1234567890" } }, request.Object));
            Assert.ThrowsException<ArgumentException>(() => ag.ProcessResponse(new NameValueCollection() { { "state", query["state"] } }, request.Object));
            Assert.ThrowsException<InvalidStateException>(() => ag.ProcessResponse(new NameValueCollection() { { "state", AuthorizationGrant.Base64UrlEncodeNoPadding(new byte[] { 0x01, 0x02, 0x03 }) }, { "code", "1234567890" } }, request.Object));
            Assert.ThrowsException<AuthorizationGrantException>(() => ag.ProcessResponse(new NameValueCollection() { { "state", query["state"] }, { "error", "error" }, { "code", "1234567890" } }, request.Object));
            Assert.ThrowsException<ArgumentException>(() => ag.ProcessResponse(new NameValueCollection() { { "state", query["state"] } }, request.Object));
        }

        [TestMethod()]
        public void Base64UrlEncodeNoPaddingTest()
        {
            Assert.AreEqual("ESM", AuthorizationGrant.Base64UrlEncodeNoPadding(new byte[] { 0x11, 0x23 }));
            Assert.AreEqual("HE3j", AuthorizationGrant.Base64UrlEncodeNoPadding(new byte[] { 0x1c, 0x4d, 0xe3 }));
            Assert.AreEqual("LqhVsL4", AuthorizationGrant.Base64UrlEncodeNoPadding(new byte[] { 0x2e, 0xa8, 0x55, 0xb0, 0xbe }));
            Assert.AreEqual("DEZGb5gDRyzWvS4oDmEwX8F-h8Lcdo6fdBgzsI_9-No", AuthorizationGrant.Base64UrlEncodeNoPadding(new byte[] {
                0x0c, 0x46, 0x46, 0x6f, 0x98, 0x03, 0x47, 0x2c, 0xd6, 0xbd, 0x2e, 0x28, 0x0e, 0x61, 0x30, 0x5f,
                0xc1, 0x7e, 0x87, 0xc2, 0xdc, 0x76, 0x8e, 0x9f, 0x74, 0x18, 0x33, 0xb0, 0x8f, 0xfd, 0xf8, 0xda,
            }));
        }

        [TestMethod()]
        public void Base64UriDecodeNoPaddingTest()
        {
            CollectionAssert.AreEqual(new byte[] { 0x11, 0x23 }, AuthorizationGrant.Base64UriDecodeNoPadding("ESM"));
            CollectionAssert.AreEqual(new byte[] { 0x1c, 0x4d, 0xe3 }, AuthorizationGrant.Base64UriDecodeNoPadding("HE3j"));
            CollectionAssert.AreEqual(new byte[] { 0x2e, 0xa8, 0x55, 0xb0, 0xbe }, AuthorizationGrant.Base64UriDecodeNoPadding("LqhVsL4"));
            CollectionAssert.AreEqual(new byte[] {
                0x0c, 0x46, 0x46, 0x6f, 0x98, 0x03, 0x47, 0x2c, 0xd6, 0xbd, 0x2e, 0x28, 0x0e, 0x61, 0x30, 0x5f,
                0xc1, 0x7e, 0x87, 0xc2, 0xdc, 0x76, 0x8e, 0x9f, 0x74, 0x18, 0x33, 0xb0, 0x8f, 0xfd, 0xf8, 0xda,
            }, AuthorizationGrant.Base64UriDecodeNoPadding("DEZGb5gDRyzWvS4oDmEwX8F-h8Lcdo6fdBgzsI_9-No"));
        }
    }
}

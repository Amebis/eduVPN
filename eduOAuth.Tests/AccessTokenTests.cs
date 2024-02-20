/*
    eduOAuth - OAuth 2.0 Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace eduOAuth.Tests
{
    [TestClass()]
    public class AccessTokenTests
    {
        [TestMethod()]
        public void AccessTokenSerializationTest()
        {
            AccessToken
                token1 = new BearerToken(Global.AccessTokenObj, DateTimeOffset.Now),
                token2 = AccessToken.FromBase64String(token1.ToBase64String());
            Assert.AreEqual(token1, token2);
            Assert.IsTrue(token1.Authorized == token2.Authorized);
            Assert.IsTrue(token1.Expires == token2.Expires);
            Assert.IsTrue((token1.Scope == null) == (token2.Scope == null));
            Assert.IsTrue(token1.Scope == null || token1.Scope.SetEquals(token2.Scope));
        }

        [TestMethod()]
        public void AccessTokenFromAuthorizationServerTest()
        {
            var response = new Mock<HttpWebResponse>();
            response.Setup(obj => obj.GetResponseStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes(Global.AccessTokenJSON)));
            response.SetupGet(obj => obj.StatusCode).Returns(HttpStatusCode.OK);
            var request = new Mock<HttpWebRequest>();
            request.Setup(obj => obj.GetResponse()).Returns(response.Object);

            var now = DateTimeOffset.Now;
            AccessToken
                token1 = new BearerToken(Global.AccessTokenObj, now),
                token2 = AccessToken.FromAuthorizationServerResponse(request.Object, now);
            Assert.AreEqual(token1, token2);
            Assert.IsTrue(token1.Authorized == token2.Authorized);
            Assert.IsTrue((token1.Expires - token2.Expires).TotalSeconds < 60);
            Assert.IsTrue((token1.Scope == null) == (token2.Scope == null));
            Assert.IsTrue(token1.Scope == null || token1.Scope.SetEquals(token2.Scope));
        }

        [TestMethod()]
        public void AccessTokenFromAuthorizationServerExceptionTest()
        {
            var response = new Mock<HttpWebResponse>();
            response.Setup(obj => obj.GetResponseStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes("{\"error\":\"test_error\",\"error_description\":\"Test Error\",\"error_uri\":\"http://www.foobar.org/\"}")));
            response.SetupGet(obj => obj.ContentType).Returns("application/json");
            response.SetupGet(obj => obj.StatusCode).Returns(HttpStatusCode.BadRequest);
            var request = new Mock<HttpWebRequest>();
            request.Setup(obj => obj.GetResponse()).Throws(new WebException("Test Exception", null, WebExceptionStatus.Success, response.Object));

            Assert.ThrowsException<AccessTokenException>(() => AccessToken.FromAuthorizationServerResponse(request.Object, DateTimeOffset.Now));
        }

        [TestMethod()]
        public void AccessTokenRefreshTest()
        {
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
                token2 = token1.RefreshToken(request.Object, "org.eduvpn.app.windows", new NetworkCredential("username", "password"));
            var request_param = HttpUtility.ParseQueryString(Encoding.ASCII.GetString(request_buffer, 0, (int)request.Object.ContentLength));
            Assert.AreEqual("refresh_token", request_param["grant_type"]);
            Assert.IsNotNull(request_param["refresh_token"]);
            Assert.IsTrue((token1.Scope == null) == (request_param["scope"] == null));
            Assert.IsTrue(token1.Scope == null || token1.Scope.SetEquals(new HashSet<string>(request_param["scope"].Split(null))));
            Assert.AreEqual(token1, token2);
            Assert.IsTrue(token1.Authorized == token2.Authorized);
            Assert.IsTrue((token1.Expires - token2.Expires).TotalSeconds < 60);
            Assert.IsTrue((token1.Scope == null) == (token2.Scope == null));
            Assert.IsTrue(token1.Scope == null || token1.Scope.SetEquals(token2.Scope));
        }
    }
}

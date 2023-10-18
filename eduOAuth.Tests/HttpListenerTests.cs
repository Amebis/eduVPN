/*
    eduOAuth - OAuth 2.0 Library for eduVPN (and beyond)

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Net;
using System.Text;

namespace eduOAuth.Tests
{
    [TestClass()]
    public class HttpListenerTests
    {
        [TestMethod()]
        public void HttpListenerTest()
        {
            string uriBase = null;
            var callbackCalled = false;
            var listener = new HttpListener(IPAddress.Loopback, 0);
            listener.HttpCallback += (object sender, HttpCallbackEventArgs e) =>
            {
                Assert.AreEqual(uriBase + "/callback?test123", e.Uri.AbsoluteUri);
                callbackCalled = true;
            };

            listener.Start();
            try
            {
                uriBase = string.Format("http://{0}:{1}", IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);

                {
                    var request = (HttpWebRequest)WebRequest.Create(uriBase + "/callback?test123");
                    request.Method = "POST";
                    request.ContentType = "text/plain";
                    var data = Encoding.ASCII.GetBytes("This is a test content.");
                    using (var requestStream = request.GetRequestStream())
                        requestStream.Write(data, 0, data.Length);

                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        Assert.IsTrue(callbackCalled);
                        Assert.AreEqual("text/html; charset=UTF-8", response.ContentType);

                        using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8, false))
                            reader.ReadToEnd();
                    }
                }

                {
                    var request = (HttpWebRequest)WebRequest.Create(uriBase + "/finished");
                    using (var response = (HttpWebResponse)request.GetResponse())
                        Assert.AreEqual("text/html; charset=UTF-8", response.ContentType);
                }

                {
                    var request = (HttpWebRequest)WebRequest.Create(uriBase + "/script.js");
                    using (var response = (HttpWebResponse)request.GetResponse())
                        Assert.AreEqual("text/javascript", response.ContentType);
                }

                {
                    var request = (HttpWebRequest)WebRequest.Create(uriBase + "/style.css");
                    using (var response = (HttpWebResponse)request.GetResponse())
                        Assert.AreEqual("text/css", response.ContentType);
                }

                {
                    var request = (HttpWebRequest)WebRequest.Create(uriBase + "/nonexisting");
                    try
                    {
                        using (request.GetResponse())
                        {
                        }
                        Assert.Fail("\"404\" tolerated");
                    }
                    catch (WebException ex)
                    {
                        Assert.IsTrue(ex.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.NotFound);
                    }
                }
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}

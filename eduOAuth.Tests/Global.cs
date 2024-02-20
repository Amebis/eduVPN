/*
    eduOAuth - OAuth 2.0 Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;

namespace eduOAuth.Tests
{
    static class Global
    {
        public const string AccessTokenJSON = "{\"access_token\":\"dxG1Z07kbr15a8nypuCk7OSf2USL7DlqMJCSicSR1oX\\/6EX3UJE6iboB78EeQwol4zZm7uKaT7B9tk0LZl2XBHsidHlwZSI6ImFjY2Vzc190b2tlbiIsImF1dGhfa2V5IjoiYzQxNDdmMmRlZjljM2ZlOTkzMThlMjEyMzc5NGE5MTMiLCJ1c2VyX2lkIjoiZDRhYzZiMDQ0MzA1NWZiNWE3MTQyMDM3ZDZhZGZiMzU1OGNiYzcxZCIsImNsaWVudF9pZCI6Im9yZy5lZHV2cG4uYXBwLndpbmRvd3MiLCJzY29wZSI6ImNvbmZpZyIsImV4cGlyZXNfYXQiOiIyMDE4LTAxLTA5IDEwOjQ4OjU5In0=\",\"refresh_token\":\"E\\/7pOay3LzBDA+WHsC78q60I6ujnbwqnAVA8ac2e07eFYfS4gApR1K+rwt5DUaERj5xjkguVqliNO2HoPQYxAHsidHlwZSI6InJlZnJlc2hfdG9rZW4iLCJhdXRoX2tleSI6ImM0MTQ3ZjJkZWY5YzNmZTk5MzE4ZTIxMjM3OTRhOTEzIiwidXNlcl9pZCI6ImQ0YWM2YjA0NDMwNTVmYjVhNzE0MjAzN2Q2YWRmYjM1NThjYmM3MWQiLCJjbGllbnRfaWQiOiJvcmcuZWR1dnBuLmFwcC53aW5kb3dzIiwic2NvcGUiOiJjb25maWcifQ==\",\"token_type\":\"bearer\",\"scope\":\"test1 test2\",\"expires_in\":3600}";

        public static readonly Dictionary<string, object> AccessTokenObj = (Dictionary<string, object>)eduJSON.Parser.Parse(AccessTokenJSON);
    }
}

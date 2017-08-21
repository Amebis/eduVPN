/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN list of instances using federated authentication
    /// </summary>
    /// <remarks>
    /// Access token is issued by a central OAuth server; all instances accept this token.
    /// </remarks>
    public class FederatedInstanceGroupInfo : InstanceGroupInfo
    {
        #region Properties

        /// <summary>
        /// Authorization endpoint URI - used by the client to obtain authorization from the resource owner via user-agent redirection.
        /// </summary>
        public Uri AuthorizationEndpoint { get => _authorization_endpoint; }
        private Uri _authorization_endpoint;

        /// <summary>
        /// Token endpoint URI - used by the client to exchange an authorization grant for an access token, typically with client authentication.
        /// </summary>
        public Uri TokenEndpoint { get => _token_endpoint; }
        private Uri _token_endpoint;

        #endregion

        #region ILoadableItem Support

        public override void Load(object obj)
        {
            base.Load(obj);

            if (obj is Dictionary<string, object> obj2)
            {
                // Set authorization endpoint.
                _authorization_endpoint = new Uri(eduJSON.Parser.GetValue<string>(obj2, "authorization_endpoint"));

                // Set token endpoint.
                _token_endpoint = new Uri(eduJSON.Parser.GetValue<string>(obj2, "token_endpoint"));
            }
            else
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(Dictionary<string, object>), obj.GetType());
        }

        #endregion
    }
}

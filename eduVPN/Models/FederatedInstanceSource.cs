/*
    eduVPN - VPN for education and research

    Copyright: 2017-2018 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;
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
    public class FederatedInstanceSource : InstanceSource
    {
        #region Methods

        /// <inheritdoc/>
        public override Instance GetAuthenticatingInstance(Instance connecting_instance)
        {
            return AuthenticatingInstance;
        }

        /// <inheritdoc/>
        public override void FromSettings(ConnectWizard wizard, Xml.InstanceSourceSettingsBase settings)
        {
            if (settings is Xml.FederatedInstanceSourceSettings h_federated)
            {
                // - Restore connecting instance (optional).
                ConnectingInstance = SelectConnectingInstance(h_federated.ConnectingInstance);
            }
        }

        /// <inheritdoc/>
        public override Xml.InstanceSourceSettingsBase ToSettings()
        {
            return
                new Xml.FederatedInstanceSourceSettings()
                {
                    ConnectingInstance = ConnectingInstance?.Base
                };
        }

        #endregion

        #region ILoadableItem Support

        /// <inheritdoc/>
        public override void Load(object obj)
        {
            base.Load(obj);

            if (obj is Dictionary<string, object> obj2)
            {
                AuthenticatingInstance = new Instance(
                    new Uri(eduJSON.Parser.GetValue<string>(obj2, "authorization_endpoint")),
                    new Uri(eduJSON.Parser.GetValue<string>(obj2, "token_endpoint")));
            }
            else
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());
        }

        #endregion
    }
}

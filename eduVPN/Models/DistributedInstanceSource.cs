/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;
using System.Linq;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN list of instances using distributed authentication
    /// </summary>
    /// <remarks>
    /// Access token from any instance can be used by any other instance.
    /// </remarks>
    public class DistributedInstanceSource : InstanceSource
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
            if (settings is Xml.DistributedInstanceSourceSettings h_distributed)
            {
                // - Restore authenticating instance.
                // - Restore connecting instance (optional).
                AuthenticatingInstance = h_distributed.AuthenticatingInstance != null ? InstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_distributed.AuthenticatingInstance.AbsoluteUri) : null;
                if (AuthenticatingInstance != null)
                {
                    AuthenticatingInstance.RequestAuthorization += wizard.Instance_RequestAuthorization;
                    AuthenticatingInstance.ForgetAuthorization += wizard.Instance_ForgetAuthorization;
                    ConnectingInstance = h_distributed.ConnectingInstance != null ? ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_distributed.ConnectingInstance.AbsoluteUri) : null;
                }
            }
        }

        /// <inheritdoc/>
        public override Xml.InstanceSourceSettingsBase ToSettings()
        {
            return
                new Xml.DistributedInstanceSourceSettings()
                {
                    AuthenticatingInstance = AuthenticatingInstance?.Base,
                    ConnectingInstance = ConnectingInstance?.Base
                };
        }

        #endregion
    }
}

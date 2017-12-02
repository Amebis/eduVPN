/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;

namespace eduVPN.ViewModels.Panels
{
    /// <summary>
    /// Connecting instance select panel
    /// </summary>
    public class ConnectingInstanceSelectPanel : ConnectingSelectPanel
    {
        #region Constructors

        /// <summary>
        /// Constructs a panel
        /// </summary>
        /// <param name="parent">The page parent</param>
        /// <param name="instance_source_type">Instance source type</param>
        public ConnectingInstanceSelectPanel(ConnectWizard parent, InstanceSourceType instance_source_type) :
            base(parent, instance_source_type)
        {
        }

        #endregion
    }
}

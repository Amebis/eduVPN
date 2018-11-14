﻿/*
    eduVPN - VPN for education and research

    Copyright: 2017-2018 The Commons Conservancy eduVPN Programme
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
        /// <param name="wizard">The connecting wizard</param>
        /// <param name="instance_source_type">Instance source type</param>
        public ConnectingInstanceSelectPanel(ConnectWizard wizard, InstanceSourceType instance_source_type) :
            base(wizard, instance_source_type)
        {
        }

        #endregion
    }
}

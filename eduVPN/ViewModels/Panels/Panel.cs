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
    /// Panel base class
    /// </summary>
    public class Panel : ValidatableBindableBase
    {
        #region Properties

        /// <summary>
        /// The panel parent
        /// </summary>
        public ConnectWizard Parent { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Construct a panel
        /// </summary>
        /// <param name="parent">The panel parent</param>
        public Panel(ConnectWizard parent)
        {
            Parent = parent;
        }

        #endregion
    }
}

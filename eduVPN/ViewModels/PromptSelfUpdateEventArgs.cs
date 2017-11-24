/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// PromptSelfUpdate event arguments
    /// </summary>
    public class PromptSelfUpdateEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Installed product version
        /// </summary>
        public Version InstalledVersion { get; }

        /// <summary>
        /// Available product version
        /// </summary>
        public Version AvailableVersion { get; }

        /// <summary>
        /// Action instructed by user
        /// </summary>
        /// <remarks>Should be populated by action on event end.</remarks>
        public PromptSelfUpdateActionType Action { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs event arguments
        /// </summary>
        /// <param name="installed_version">Installed product version</param>
        /// <param name="available_version">Available product version</param>
        public PromptSelfUpdateEventArgs(Version installed_version, Version available_version)
        {
            InstalledVersion = installed_version;
            AvailableVersion = available_version;
        }

        #endregion
    }
}

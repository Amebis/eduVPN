/*
    eduVPN - VPN for education and research

    Copyright: 2017-2018 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduVPN.Models
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
        /// Product changelog
        /// </summary>
        public Uri ChangelogPath { get; }

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
        /// <param name="changelog_path">Product changelog</param>
        public PromptSelfUpdateEventArgs(Version installed_version, Version available_version, Uri changelog_path)
        {
            InstalledVersion = installed_version;
            AvailableVersion = available_version;
            ChangelogPath = changelog_path;
        }

        #endregion
    }
}

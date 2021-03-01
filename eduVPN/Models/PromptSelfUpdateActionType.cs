/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.Models
{
    /// <summary>
    /// User actions when an update is available
    /// </summary>
    public enum PromptSelfUpdateActionType
    {
        /// <summary>
        /// Ask to update again later (default)
        /// </summary>
        AskLater = 0,

        /// <summary>
        /// Update now
        /// </summary>
        Update,

        /// <summary>
        /// Skip this update version
        /// </summary>
        Skip,
    }
}

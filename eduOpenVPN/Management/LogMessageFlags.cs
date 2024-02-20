/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduOpenVPN.Management
{
    /// <summary>
    /// OpenVPN log message flags
    /// </summary>
    [Flags]
    public enum LogMessageFlags
    {
        /// <summary>
        /// Informational
        /// </summary>
        Informational = 1 << 0, // 1

        /// <summary>
        /// Fatal error
        /// </summary>
        FatalError = 1 << 1, // 2

        /// <summary>
        /// Non-fatal error
        /// </summary>
        NonFatalError = 1 << 2, // 4

        /// <summary>
        /// Warning
        /// </summary>
        Warning = 1 << 3, // 8

        /// <summary>
        /// Debug
        /// </summary>
        Debug = 1 << 4, // 16
    }
}

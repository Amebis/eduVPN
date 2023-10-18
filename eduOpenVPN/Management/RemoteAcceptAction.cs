/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduOpenVPN.Management
{
    /// <summary>
    /// OpenVPN Management session remote "ACCEPT" command action
    /// </summary>
    public class RemoteAcceptAction : RemoteAction
    {
        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return "ACCEPT";
        }

        #endregion
    }
}

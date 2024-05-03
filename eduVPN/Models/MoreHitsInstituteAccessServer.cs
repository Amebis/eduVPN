/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.Models
{
    /// <summary>
    /// A placeholder for institute access server lists when too many to display all.
    /// </summary>
    public class MoreHitsInstituteAccessServer : InstituteAccessServer
    {
        #region Constructors

        public MoreHitsInstituteAccessServer() { }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return Resources.Strings.TooManyResults;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null || GetType() != obj.GetType())
                return false;
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return 0;
        }

        #endregion
    }
}

/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.JSON
{
    /// <summary>
    /// A JSON loadable item
    /// </summary>
    public interface ILoadableItem
    {
        /// <summary>
        /// Loads class from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">A dictionary object</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException">Incorrect parameter <paramref name="obj"/> type</exception>
        void Load(object obj);
    }
}

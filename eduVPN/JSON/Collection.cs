/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace eduVPN.JSON
{
    /// <summary>
    /// Collection of loadable JSON items
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class Collection<T> : ObservableCollection<T>, ILoadableItem where T : ILoadableItem, new()
    {
        #region Methods

        /// <summary>
        /// Loads item list from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">List of items.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>List&lt;object&gt;</c></exception>
        public void Load(object obj)
        {
            var obj2 = obj as List<object>;
            if (obj2 == null)
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(List<object>), obj.GetType());

            Clear();

            // Parse all items listed. Don't do it in parallel to preserve the sort order.
            foreach (var el in obj2)
            {
                var item = new T();
                item.Load(el);
                Add(item);
            }
        }

        #endregion
    }
}

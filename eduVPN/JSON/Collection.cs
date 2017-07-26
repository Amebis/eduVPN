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
    public class Collection<T> : ObservableCollection<T>, ILoadableItem where T : ILoadableItem, new()
    {
        #region Methods

        /// <summary>
        /// Loads item list from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">List of items</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>List&lt;object&gt;</c></exception>
        public virtual void Load(object obj)
        {
            if (obj is List<object> obj2)
            {
                Clear();

                // Parse all items listed. Don't do it in parallel to preserve the sort order.
                foreach (var el in obj2)
                {
                    var item = new T();
                    item.Load(el);
                    Add(item);
                }
            }
            else
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(List<object>), obj.GetType());
        }

        #endregion
    }
}

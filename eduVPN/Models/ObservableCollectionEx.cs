/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace eduVPN.Models
{
    /// <summary>
    /// An ObservableCollection derived class to provide bulk operations
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    public class ObservableCollectionEx<T> : ObservableCollection<T>
    {
        #region Constructors

        /// <inheritdoc/>
        public ObservableCollectionEx() : base()
        {
        }

        /// <inheritdoc/>
        public ObservableCollectionEx(IEnumerable<T> collection) : base(collection)
        {
        }

        /// <inheritdoc/>
        public ObservableCollectionEx(List<T> list) : base(list)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Bulk-adds a range of items, then fires notifications.
        /// </summary>
        /// <param name="range">Range of elements</param>
        public void AddRange(IEnumerable<T> range)
        {
            foreach (var item in range)
                Items.Add(item);
            EndUpdate();
        }

        /// <summary>
        /// Bulk-replaces list with a range of items, then fires notifications.
        /// </summary>
        /// <param name="range">Range of elements</param>
        public void Reset(IEnumerable<T> range)
        {
            Items.Clear();
            AddRange(range);
        }

        /// <summary>
        /// Bulk-replaces list with an element, then fires notifications.
        /// </summary>
        /// <param name="el">Elements</param>
        public void Reset(T el)
        {
            Items.Clear();
            Items.Add(el);
            EndUpdate();
        }

        /// <summary>
        /// Begins bulk update
        /// </summary>
        /// <returns>Internal item list</returns>
        public IList<T> BeginUpdate()
        {
            return Items;
        }

        /// <summary>
        /// Fires notifications after bulk update is complete.
        /// </summary>
        public void EndUpdate()
        {
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        #endregion
    }
}

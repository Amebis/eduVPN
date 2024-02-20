/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace eduVPN.Models
{
    /// <summary>
    /// An ObservableCollection derived class to provide bulk operations
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        #region Fields

        Dictionary<TKey, TValue> Dict = new Dictionary<TKey, TValue>();

        #endregion

        #region IDictionary Support

        public TValue this[TKey key]
        {
            get => Dict[key];
            set
            {
                var count = Dict.Count;
                Dict[key] = value;
                if (count != Dict.Count)
                {
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, key));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Keys)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Values)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
                }
                else
                {
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, key));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Values)));
                }
            }
        }

        public ICollection<TKey> Keys => Dict.Keys;

        public ICollection<TValue> Values => Dict.Values;

        public int Count => Dict.Count;

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value) => Dict[key] = value;

        public void Add(KeyValuePair<TKey, TValue> item) => Dict[item.Key] = item.Value;

        public void Clear()
        {
            Dict.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Keys)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Values)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) => Dict.ContainsKey(item.Key);

        public bool ContainsKey(TKey key) => Dict.ContainsKey(key);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException(nameof(array));
            foreach (var pair in Dict)
                array[arrayIndex++] = pair;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Dict.GetEnumerator();

        public bool Remove(TKey key)
        {
            if (Dict.TryGetValue(key, out var _))
            {
                Dict.Remove(key);
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, key));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Keys)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Values)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
                return true;
            }
            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

        public bool TryGetValue(TKey key, out TValue value) => Dict.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => Dict.GetEnumerator();

        #endregion

        #region INotifyCollectionChanged Support

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region INotifyPropertyChanged Support

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}

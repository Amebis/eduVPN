/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace eduVPN.Models
{
    public static class Extensions
    {
        /// <summary>
        /// Loads class from a JSON string
        /// </summary>
        /// <param name="i">Loadable item</param>
        /// <param name="json">JSON string</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        public static void LoadJSON<T>(this ObservableCollection<T> i, string json, CancellationToken ct = default) where T : ILoadableItem, new()
        {
            var obj = eduJSON.Parser.Parse(json, ct);
            if (!(obj is List<object> obj2))
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(List<object>), obj.GetType());

            // Parse all items listed. Don't do it in parallel to preserve the sort order.
            i.Clear();
            foreach (var el in obj2)
            {
                var item = new T();
                item.Load(el);
                i.Add(item);
            }
        }

        /// <summary>
        /// Loads class from a JSON string provided by API
        /// </summary>
        /// <param name="i">Loadable item</param>
        /// <param name="json">JSON string representing a dictionary of key/values with <paramref name="name"/> element</param>
        /// <param name="name">The name of the value holder element</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        public static void LoadJSONAPIResponse<T>(this ObservableCollection<T> i, string json, string name, CancellationToken ct = default) where T : ILoadableItem, new()
        {
            // Parse JSON string and get inner key/value dictionary.
            var obj = eduJSON.Parser.GetValue<Dictionary<string, object>>(
                (Dictionary<string, object>)eduJSON.Parser.Parse(json, ct),
                name);

            // Verify response status.
            if (eduJSON.Parser.GetValue(obj, "ok", out bool ok) && !ok)
                throw new APIErrorException();

            // Load collection.
            if (!(obj["data"] is List<object> obj2))
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(List<object>), obj.GetType());

            // Parse all items listed. Don't do it in parallel to preserve the sort order.
            i.Clear();
            foreach (var el in obj2)
            {
                var item = new T();
                item.Load(el);
                i.Add(item);
            }
        }
    }
}

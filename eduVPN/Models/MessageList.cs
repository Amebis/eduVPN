/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace eduVPN.Models
{
    /// <summary>
    /// eduVPN user/system message list
    /// </summary>
    public class MessageList : ObservableCollection<Message>, JSON.ILoadableItem
    {
        #region Methods

        /// <summary>
        /// Loads class from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">A dictionary object</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException">Incorrect parameter <paramref name="obj"/> type</exception>
        public void Load(object obj)
        {
            if (obj is List<object> obj2)
            {
                Clear();

                // Parse all mesages listed. Don't do it in parallel to preserve the sort order.
                foreach (var el in obj2)
                {
                    var el2 = (Dictionary<string, object>)el;

                    // Parse message type.
                    Message message = null;
                    if (eduJSON.Parser.GetValue(el2, "type", out string type))
                    {
                        switch (type.ToLower())
                        {
                            case "motd": message = new MessageOfTheDay(); break;
                            case "maintenance": message = new MessageMaintenance(); break;
                            default: message = new MessageNotification(); break; // Assume notification type on all other values.
                        }
                    }
                    else
                        message = new MessageNotification();

                    // Load and add message.
                    message.Load(el);
                    Add(message);
                }
            }
            else
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(List<object>), obj.GetType());
        }

        #endregion
    }
}

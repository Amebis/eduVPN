/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// eduVPN user/system message list
    /// </summary>
    public class MessageList : JSON.Collection<Message>
    {
        #region Methods

        public override void Load(object obj)
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
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(List<object>), obj.GetType());
        }

        #endregion
    }
}

/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using Prism.Mvvm;
using System.Collections.Generic;

namespace eduVPN.JSON
{
    /// <summary>
    /// eduVPN user/system message
    /// </summary>
    public class Message : BindableBase, ILoadableItem
    {
        #region Properties

        /// <summary>
        /// Message text
        /// </summary>
        public string Text
        {
            get { return _text; }
            set { if (value != _text) { _text = value; RaisePropertyChanged(); } }
        }
        private string _text;

        /// <summary>
        /// Message date and time
        /// </summary>
        public DateTime Date
        {
            get { return _date; }
            set { if (value != _date) { _date = value; RaisePropertyChanged(); } }
        }
        private DateTime _date;

        /// <summary>
        /// Message period begin time
        /// </summary>
        public DateTime? Begin
        {
            get { return _begin; }
            set { if (value != _begin) { _begin = value; RaisePropertyChanged(); } }
        }
        private DateTime? _begin;

        /// <summary>
        /// Message period end time
        /// </summary>
        public DateTime? End
        {
            get { return _end; }
            set { if (value != _end) { _end = value; RaisePropertyChanged(); } }
        }
        private DateTime? _end;

        /// <summary>
        /// Message type
        /// </summary>
        public MessageType Type
        {
            get { return _type; }
            set { if (value != _type) { _type = value; RaisePropertyChanged(); } }
        }
        private MessageType _type;

        #endregion

        #region Methods

        public override string ToString()
        {
            return Text;
        }

        /// <summary>
        /// Loads message from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>message</c>, <c>date_time</c>, <c>begin</c>, <c>end</c>, and <c>type</c> elements. <c>message</c> and <c>date_time</c> are required. All elements should be strings.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public void Load(object obj)
        {
            var obj2 = obj as Dictionary<string, object>;
            if (obj2 == null)
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(Dictionary<string, object>), obj.GetType());

            // Set message text.
            Text = eduJSON.Parser.GetValue<string>(obj2, "message");

            // Set message dates.
            Date = DateTime.Parse(eduJSON.Parser.GetValue<string>(obj2, "date_time"));
            Begin = eduJSON.Parser.GetValue(obj2, "begin", out string begin) && DateTime.TryParse(begin, out var begin_date)? begin_date : (DateTime?)null;
            End   = eduJSON.Parser.GetValue(obj2, "end"  , out string end  ) && DateTime.TryParse(end  , out var end_date  )? end_date   : (DateTime?)null;

            // Parse message type.
            if (eduJSON.Parser.GetValue(obj2, "type", out string type))
            {
                switch (type.ToLower())
                {
                    case "motd": Type = MessageType.MotD; break;
                    case "maintenance": Type = MessageType.Maintenance; break;
                    default: Type = MessageType.Notification; break; // Assume notification type on all other values.
                }
            }
            else
                Type = MessageType.Notification;
        }

        #endregion
    }
}

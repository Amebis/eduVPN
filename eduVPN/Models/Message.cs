﻿/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Diagnostics;

namespace eduVPN.Models
{
    /// <summary>
    /// eduVPN user/system message base class
    /// </summary>
    public class Message : BindableBase, JSON.ILoadableItem
    {
        #region Properties

        /// <summary>
        /// Message text
        /// </summary>
        public string Text { get => _text; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _text;

        /// <summary>
        /// Message date and time
        /// </summary>
        public DateTime Date { get => _date; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DateTime _date;

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return Text;
        }

        #endregion

        #region ILoadableItem Support

        /// <summary>
        /// Loads message from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>message</c>, <c>date_time</c>, <c>begin</c>, <c>end</c>, and <c>type</c> elements. <c>message</c> and <c>date_time</c> are required. All elements should be strings.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public virtual void Load(object obj)
        {
            if (!(obj is Dictionary<string, object> obj2))
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());

            // Set message text.
            SetProperty(
                ref _text,
                eduJSON.Parser.GetDictionary<string>(obj2, "message").GetLocalized(),
                nameof(Text));

            // Set message dates.
            SetProperty(
                ref _date,
                DateTime.Parse(eduJSON.Parser.GetValue<string>(obj2, "date_time")),
                nameof(Date));
        }

        #endregion
    }
}

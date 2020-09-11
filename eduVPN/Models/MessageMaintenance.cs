/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace eduVPN.Models
{
    /// <summary>
    /// eduVPN system maintenance message
    /// </summary>
    public class MessageMaintenance : Message
    {
        #region Properties

        /// <summary>
        /// Maintenance period begin time
        /// </summary>
        public DateTime? Begin
        {
            get { return _begin; }
            set { SetProperty(ref _begin, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DateTime? _begin;

        /// <summary>
        /// Maintenance period end time
        /// </summary>
        public DateTime? End
        {
            get { return _end; }
            set { SetProperty(ref _end, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DateTime? _end;

        #endregion

        #region ILoadableItem Support

        /// <summary>
        /// Loads message from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>message</c>, <c>date_time</c>, <c>begin</c>, <c>end</c>, and <c>type</c> elements. <c>message</c> and <c>date_time</c> are required. All elements should be strings.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public override void Load(object obj)
        {
            base.Load(obj);

            if (!(obj is Dictionary<string, object> obj2))
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());

            // Set message dates.
            Begin = eduJSON.Parser.GetValue(obj2, "begin", out string begin) && DateTime.TryParse(begin, out var begin_date) ? begin_date : (DateTime?)null;
            End = eduJSON.Parser.GetValue(obj2, "end", out string end) && DateTime.TryParse(end, out var end_date) ? end_date : (DateTime?)null;
        }

        #endregion
    }
}

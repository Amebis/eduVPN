/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;

namespace eduVPN.Localization
{
    /// <summary>
    /// Converts {0:second|seconds} to "1 second", "2 seconds", "3 seconds"...
    /// </summary>
    public class CardinalPluralFormatProvider : IFormatProvider, ICustomFormatter
    {
        #region Fields

        /// <summary>
        /// Dictionary of language-pluralizer pairs
        /// </summary>
        private static readonly Dictionary<string, Func<int, int>> CardinalPluralizers = new Dictionary<string, Func<int, int>>()
        {
            // Default pluralizer
            { "",  n => n == 1 ? 0 : 1 },

            // Language-specific pluralizers
            { "ar", n => n == 0 ? 0 : n == 1 ? 1 : n == 2 ? 2 : n % 100 >= 3 && n % 100 <= 10 ? 3 : n % 100 >= 11 ? 4 : 5 },
            { "fr", n => n <= 1 ? 0 : 1 },
            { "sl", n => n % 100 == 1 ? 0 : n % 100 == 2 ? 1 : n % 100 == 3 || n % 100 == 4 ? 2 : 3 },
            { "tr", n => n <= 1 ? 0 : 1 },
            { "uk", n => n % 10 == 1 && n % 100 != 11 ? 0 : n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20) ? 1 : 2 },
        };

        /// <summary>
        /// Dictionary of language-spacing pairs
        /// </summary>
        private static readonly Dictionary<string, string> CardinalSpacing = new Dictionary<string, string>()
        {
            // Default spacing
            { "",  " " },

            // Language-specific spacing
            { "ja",  "" },
        };

        #endregion

        #region Methods

        /// <inheritdoc/>
        public object GetFormat(Type formatType)
        {
            return this;
        }

        /// <inheritdoc/>
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            var forms = format.Split('|');
            if (arg is int n)
            {
                if (CardinalPluralizers.TryGetValue(System.Threading.Thread.CurrentThread.CurrentUICulture.IetfLanguageTag, out var pluralizer) ||
                    CardinalPluralizers.TryGetValue(System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName, out pluralizer) ||
                    CardinalPluralizers.TryGetValue("", out pluralizer))
                {
                    var form = pluralizer(n >= 0 ? n : -n);
                    if (form >= forms.Length)
                        throw new ArgumentException(string.Format("Numeral {0} should use {1}. plural form, but \"{2}\" provides only {3} plural form(s).", n, form + 1, format, forms.Length));
                    return
                        CardinalSpacing.TryGetValue(System.Threading.Thread.CurrentThread.CurrentUICulture.IetfLanguageTag, out var spacing) ||
                        CardinalSpacing.TryGetValue(System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName, out spacing) ||
                        CardinalSpacing.TryGetValue("", out spacing) ? n.ToString() + spacing + forms[form] : n.ToString() + " " + forms[form];
                }
            }
            return arg.ToString();
        }

        #endregion
    }
}

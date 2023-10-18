/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;

namespace eduOpenVPN
{
    /// <summary>
    /// OpenVPN configuration management helper class
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// Escapes value string to be used as a parameter in OpenVPN configuration file (.ovpn)
        /// </summary>
        /// <param name="value">Parameter value</param>
        /// <param name="force">Force quote</param>
        /// <returns>Quoted and escaped <paramref name="value"/> when escaping required; <paramref name="value"/> otherwise</returns>
        public static string EscapeParamValue(string value, bool force = false)
        {
            return value.Length > 0 ?
                force || value.IndexOfAny(new char[] { '\\', ' ', '"', '\'' }) >= 0 ?
                    "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"" : // Escape backslash and double quotes, and add surrounding quotes
                    value : // No need to escape
                    "\"\""; // Empty string
        }

        /// <summary>
        /// Parses OpenVPN command line
        /// </summary>
        /// <param name="commandLine">Command line to parse</param>
        /// <returns>List of string parameters</returns>
        /// <exception cref="ArgumentException">Command line parsing failed</exception>
        /// <remarks>This method is OpenVPN v2.5 <c>parse_line()</c> function ported to C#.</remarks>
        public static List<string> ParseParams(string commandLine)
        {
            var ret = new List<string>();
            int offset = 0, endOffset = commandLine.Length;
            var state = ParseParamsState.Initial;
            var backslash = false;
            char inChar, outChar;
            var parm = "";

            do
            {
                inChar = offset < endOffset ? commandLine[offset] : default;
                outChar = default;

                if (!backslash && inChar == '\\' && state != ParseParamsState.ReadingSingleQuotedParam)
                    backslash = true;
                else
                {
                    if (state == ParseParamsState.Initial)
                    {
                        if (!IsZeroOrWhiteChar(inChar))
                        {
                            if (inChar == ';' || inChar == '#') // comment
                                break;
                            if (!backslash && inChar == '\"')
                                state = ParseParamsState.ReadingQuotedParam;
                            else if (!backslash && inChar == '\'')
                                state = ParseParamsState.ReadingSingleQuotedParam;
                            else
                            {
                                outChar = inChar;
                                state = ParseParamsState.ReadingUnquotedParam;
                            }
                        }
                    }
                    else if (state == ParseParamsState.ReadingUnquotedParam)
                    {
                        if (!backslash && IsZeroOrWhiteChar(inChar))
                            state = ParseParamsState.Done;
                        else
                            outChar = inChar;
                    }
                    else if (state == ParseParamsState.ReadingQuotedParam)
                    {
                        if (!backslash && inChar == '\"')
                            state = ParseParamsState.Done;
                        else
                            outChar = inChar;
                    }
                    else if (state == ParseParamsState.ReadingSingleQuotedParam)
                    {
                        if (inChar == '\'')
                            state = ParseParamsState.Done;
                        else
                            outChar = inChar;
                    }

                    if (state == ParseParamsState.Done)
                    {
                        ret.Add(parm);
                        state = ParseParamsState.Initial;
                        parm = "";
                    }

                    if (backslash && outChar != default)
                    {
                        if (!(outChar == '\\' || outChar == '\"' || IsZeroOrWhiteChar(outChar)))
                            throw new ArgumentException(Resources.Strings.ErrorBadBackslash, nameof(commandLine));
                    }
                    backslash = false;
                }

                // Store parameter character.
                if (outChar != default)
                    parm += outChar;
            }
            while (offset++ < endOffset);

            switch (state)
            {
                case ParseParamsState.Initial: break;
                case ParseParamsState.ReadingQuotedParam: throw new ArgumentException(Resources.Strings.ErrorNoClosingQuotation, nameof(commandLine));
                case ParseParamsState.ReadingSingleQuotedParam: throw new ArgumentException(Resources.Strings.ErrorNoClosingSingleQuotation, nameof(commandLine));
                default: throw new ArgumentException(string.Format(Resources.Strings.ErrorResidualParseState, state), nameof(commandLine));
            }

            return ret;
        }

        /// <summary>
        /// <see cref="ParseParams"/> internal state
        /// </summary>
        private enum ParseParamsState
        {
            Initial = 0,
            ReadingQuotedParam,
            ReadingUnquotedParam,
            Done,
            ReadingSingleQuotedParam,
        };

        /// <summary>
        /// Indicates whether a Unicode character is zero or categorized as white space.
        /// </summary>
        /// <param name="c">The Unicode character to evaluate</param>
        /// <returns><c>true</c> if <paramref name="c"/> is zero or white space; <c>false</c>otherwise</returns>
        private static bool IsZeroOrWhiteChar(char c)
        {
            return c == default || char.IsWhiteSpace(c);
        }
    }
}

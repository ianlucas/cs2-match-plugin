/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System.Text.RegularExpressions;

namespace MatchPlugin;

public partial class KeyValues
{
    public static T Parse<T>(string data)
    {
        data = StripSymbols().Replace(data, "");
        int index = 0;

        void SkipWhitespace()
        {
            while (index < data.Length && char.IsWhiteSpace(data[index]))
                index++;
            if (index < data.Length - 1 && data[index] == '/' && data[index + 1] == '/')
            {
                while (index < data.Length && data[index] != '\n')
                    index++;
                SkipWhitespace();
            }
        }

        string ParseString()
        {
            if (data[index] == '"')
            {
                index++;
                string value = "";
                while (index < data.Length && data[index] != '"')
                {
                    if (data[index] == '\\')
                    {
                        index++;
                        value += data[index] == 'n' ? '\n' : value += data[index];
                        index++;
                    }
                    else
                    {
                        value += data[index];
                        index++;
                    }
                }
                if (index >= data.Length || data[index] != '"')
                    throw new Exception("Bad end of string.");
                index++;
                return value;
            }
            return "";
        }

        object ParseValue()
        {
            if (data[index] == '"')
                return ParseString();
            if (data[index] == '{')
            {
                index++;
                return ParsePairs();
            }
            if (data[index] == '}')
                return "";
            Console.WriteLine(
                data.Substring(Math.Max(0, index - 64), Math.Min(64, data.Length - index))
            );
            Console.WriteLine(new string(' ', 64) + "^");
            throw new Exception($"Unexpected character at index {index}.");
        }

        List<KeyValuePair<string, object>> ParsePairs()
        {
            var pairs = new List<KeyValuePair<string, object>>();
            while (index < data.Length)
            {
                if (data[index] == '}')
                {
                    index++;
                    return pairs;
                }
                SkipWhitespace();
                string key = ParseString();
                SkipWhitespace();
                object value = ParseValue();
                SkipWhitespace();
                pairs.Add(new KeyValuePair<string, object>(key, value));
            }
            return pairs;
        }

        Dictionary<string, object> Walk(
            Dictionary<string, object> context,
            List<KeyValuePair<string, object>> pairs
        )
        {
            foreach (var pair in pairs)
            {
                string key = pair.Key;
                object value = pair.Value;
                object newValue =
                    value is string ? value : Walk([], (List<KeyValuePair<string, object>>)value);
                if (newValue is Dictionary<string, object> dictionary)
                {
                    if (!context.ContainsKey(key) || context[key] is not Dictionary<string, object>)
                        context[key] = new Dictionary<string, object>();
                    foreach (var kvp in dictionary)
                        ((Dictionary<string, object>)context[key])[kvp.Key] = kvp.Value;
                }
                else
                    context[key] = newValue;
            }
            return context;
        }
        return (T)(object)Walk([], ParsePairs());
    }

    [GeneratedRegex(@"\[[\$!][^\]]+\]")]
    private static partial Regex StripSymbols();
}

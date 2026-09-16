using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace Win98Ai
{
    public static class Json
    {
        public static object Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            Parser parser = new Parser(text);
            object value = parser.ParseValue();
            parser.SkipWhiteSpace();
            if (!parser.AtEnd)
            {
                throw new FormatException("Unexpected data after JSON value.");
            }

            return value;
        }

        public static string Quote(string value)
        {
            if (value == null)
            {
                return "null";
            }

            StringBuilder output = new StringBuilder(value.Length + 2);
            output.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                switch (ch)
                {
                    case '"': output.Append("\\\""); break;
                    case '\\': output.Append("\\\\"); break;
                    case '\b': output.Append("\\b"); break;
                    case '\f': output.Append("\\f"); break;
                    case '\n': output.Append("\\n"); break;
                    case '\r': output.Append("\\r"); break;
                    case '\t': output.Append("\\t"); break;
                    default:
                        if (ch < 32)
                        {
                            output.Append("\\u");
                            output.Append(((int)ch).ToString("x4"));
                        }
                        else
                        {
                            output.Append(ch);
                        }
                        break;
                }
            }

            output.Append('"');
            return output.ToString();
        }

        public static Hashtable AsObject(object value)
        {
            return value as Hashtable;
        }

        public static ArrayList AsArray(object value)
        {
            return value as ArrayList;
        }

        public static string GetString(Hashtable value, string name)
        {
            if (value == null || !value.ContainsKey(name) || value[name] == null)
            {
                return null;
            }

            return value[name] as string;
        }

        public static long GetInt64(Hashtable value, string name)
        {
            if (value == null || !value.ContainsKey(name) || value[name] == null)
            {
                return 0;
            }

            object number = value[name];
            if (number is long)
            {
                return (long)number;
            }
            if (number is double)
            {
                return (long)(double)number;
            }

            return 0;
        }

        private sealed class Parser
        {
            private readonly string text;
            private int position;

            public Parser(string text)
            {
                this.text = text;
            }

            public bool AtEnd
            {
                get { return position >= text.Length; }
            }

            public void SkipWhiteSpace()
            {
                while (!AtEnd && Char.IsWhiteSpace(text[position]))
                {
                    position++;
                }
            }

            public object ParseValue()
            {
                SkipWhiteSpace();
                if (AtEnd)
                {
                    throw new FormatException("Unexpected end of JSON.");
                }

                char ch = text[position];
                if (ch == '{') return ParseObject();
                if (ch == '[') return ParseArray();
                if (ch == '"') return ParseString();
                if (ch == '-' || Char.IsDigit(ch)) return ParseNumber();
                if (Match("true")) return true;
                if (Match("false")) return false;
                if (Match("null")) return null;

                throw new FormatException("Unexpected JSON token at position " +
                    position.ToString() + ".");
            }

            private Hashtable ParseObject()
            {
                Hashtable result = new Hashtable();
                position++;
                SkipWhiteSpace();
                if (Consume('}'))
                {
                    return result;
                }

                while (true)
                {
                    SkipWhiteSpace();
                    if (AtEnd || text[position] != '"')
                    {
                        throw new FormatException("Expected a JSON object key.");
                    }

                    string name = ParseString();
                    SkipWhiteSpace();
                    Require(':');
                    result[name] = ParseValue();
                    SkipWhiteSpace();
                    if (Consume('}'))
                    {
                        return result;
                    }

                    Require(',');
                }
            }

            private ArrayList ParseArray()
            {
                ArrayList result = new ArrayList();
                position++;
                SkipWhiteSpace();
                if (Consume(']'))
                {
                    return result;
                }

                while (true)
                {
                    result.Add(ParseValue());
                    SkipWhiteSpace();
                    if (Consume(']'))
                    {
                        return result;
                    }

                    Require(',');
                }
            }

            private string ParseString()
            {
                Require('"');
                StringBuilder result = new StringBuilder();
                while (!AtEnd)
                {
                    char ch = text[position++];
                    if (ch == '"')
                    {
                        return result.ToString();
                    }
                    if (ch != '\\')
                    {
                        result.Append(ch);
                        continue;
                    }

                    if (AtEnd)
                    {
                        throw new FormatException("Incomplete JSON escape.");
                    }

                    char escaped = text[position++];
                    switch (escaped)
                    {
                        case '"': result.Append('"'); break;
                        case '\\': result.Append('\\'); break;
                        case '/': result.Append('/'); break;
                        case 'b': result.Append('\b'); break;
                        case 'f': result.Append('\f'); break;
                        case 'n': result.Append('\n'); break;
                        case 'r': result.Append('\r'); break;
                        case 't': result.Append('\t'); break;
                        case 'u': result.Append(ParseUnicodeEscape()); break;
                        default: throw new FormatException("Unknown JSON escape.");
                    }
                }

                throw new FormatException("Unterminated JSON string.");
            }

            private char ParseUnicodeEscape()
            {
                if (position + 4 > text.Length)
                {
                    throw new FormatException("Incomplete Unicode escape.");
                }

                string digits = text.Substring(position, 4);
                position += 4;
                return (char)Int32.Parse(digits, NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture);
            }

            private object ParseNumber()
            {
                int start = position;
                if (text[position] == '-') position++;
                while (!AtEnd && Char.IsDigit(text[position])) position++;

                bool floatingPoint = false;
                if (!AtEnd && text[position] == '.')
                {
                    floatingPoint = true;
                    position++;
                    while (!AtEnd && Char.IsDigit(text[position])) position++;
                }

                if (!AtEnd && (text[position] == 'e' || text[position] == 'E'))
                {
                    floatingPoint = true;
                    position++;
                    if (!AtEnd && (text[position] == '+' || text[position] == '-'))
                    {
                        position++;
                    }
                    while (!AtEnd && Char.IsDigit(text[position])) position++;
                }

                string number = text.Substring(start, position - start);
                if (floatingPoint)
                {
                    return Double.Parse(number, NumberStyles.Float,
                        CultureInfo.InvariantCulture);
                }

                return Int64.Parse(number, NumberStyles.Integer,
                    CultureInfo.InvariantCulture);
            }

            private bool Match(string word)
            {
                if (position + word.Length > text.Length)
                {
                    return false;
                }
                if (String.Compare(text, position, word, 0, word.Length, false) != 0)
                {
                    return false;
                }

                position += word.Length;
                return true;
            }

            private bool Consume(char expected)
            {
                if (!AtEnd && text[position] == expected)
                {
                    position++;
                    return true;
                }
                return false;
            }

            private void Require(char expected)
            {
                if (!Consume(expected))
                {
                    throw new FormatException("Expected '" + expected.ToString() +
                        "' at position " + position.ToString() + ".");
                }
            }
        }
    }
}

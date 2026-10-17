using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NodeGraphAvr.Editor
{
    public static class JsonParser
    {
        public static JsonValue Parse(string text)
        {
            if (text == null)
                throw new JsonParseException("Input is null", 1, 1);

            var cursor = new Cursor(text);
            cursor.SkipWhitespace();
            var root = ParseValue(cursor);
            cursor.SkipWhitespace();
            if (!cursor.AtEnd)
                throw cursor.Error("Unexpected content after root value");
            return root;
        }

        private static JsonValue ParseValue(Cursor cursor)
        {
            var c = cursor.Peek();
            switch (c)
            {
                case '{': return ParseObject(cursor);
                case '[': return ParseArray(cursor);
                case '"': return new JsonValue { Kind = JsonKind.String, String = ParseString(cursor) };
                case 't': return ParseLiteral(cursor, "true", new JsonValue { Kind = JsonKind.Bool, Bool = true });
                case 'f': return ParseLiteral(cursor, "false", new JsonValue { Kind = JsonKind.Bool, Bool = false });
                case 'n': return ParseLiteral(cursor, "null", new JsonValue { Kind = JsonKind.Null });
                default:
                    if (c == '-' || (c >= '0' && c <= '9'))
                        return ParseNumber(cursor);
                    throw cursor.Error($"Unexpected character '{c}'");
            }
        }

        private static JsonValue ParseObject(Cursor cursor)
        {
            cursor.Consume();
            var result = new JsonValue { Kind = JsonKind.Object, Object = new Dictionary<string, JsonValue>() };
            cursor.SkipWhitespace();
            if (cursor.Peek() == '}')
            {
                cursor.Consume();
                return result;
            }

            while (true)
            {
                cursor.SkipWhitespace();
                if (cursor.Peek() != '"')
                    throw cursor.Error("Expected property name string");
                var key = ParseString(cursor);
                cursor.SkipWhitespace();
                if (cursor.Peek() != ':')
                    throw cursor.Error("Expected ':' after property name");
                cursor.Consume();
                cursor.SkipWhitespace();
                result.Object[key] = ParseValue(cursor);
                cursor.SkipWhitespace();
                var next = cursor.Peek();
                if (next == ',')
                {
                    cursor.Consume();
                    continue;
                }

                if (next == '}')
                {
                    cursor.Consume();
                    return result;
                }

                throw cursor.Error("Expected ',' or '}' in object");
            }
        }

        private static JsonValue ParseArray(Cursor cursor)
        {
            cursor.Consume();
            var result = new JsonValue { Kind = JsonKind.Array, Array = new List<JsonValue>() };
            cursor.SkipWhitespace();
            if (cursor.Peek() == ']')
            {
                cursor.Consume();
                return result;
            }

            while (true)
            {
                cursor.SkipWhitespace();
                result.Array.Add(ParseValue(cursor));
                cursor.SkipWhitespace();
                var next = cursor.Peek();
                if (next == ',')
                {
                    cursor.Consume();
                    continue;
                }

                if (next == ']')
                {
                    cursor.Consume();
                    return result;
                }

                throw cursor.Error("Expected ',' or ']' in array");
            }
        }

        private static string ParseString(Cursor cursor)
        {
            cursor.Consume();
            var builder = new StringBuilder();
            while (true)
            {
                if (cursor.AtEnd)
                    throw cursor.Error("Unterminated string");
                var c = cursor.Consume();
                if (c == '"')
                    return builder.ToString();
                if (c == '\\')
                {
                    if (cursor.AtEnd)
                        throw cursor.Error("Unterminated escape sequence");
                    var escape = cursor.Consume();
                    switch (escape)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u': builder.Append(ParseUnicodeEscape(cursor)); break;
                        default: throw cursor.Error($"Invalid escape '\\{escape}'");
                    }

                    continue;
                }

                if (c == '\n')
                    throw cursor.Error("Unterminated string");
                builder.Append(c);
            }
        }

        private static char ParseUnicodeEscape(Cursor cursor)
        {
            var code = 0;
            for (var i = 0; i < 4; i++)
            {
                if (cursor.AtEnd)
                    throw cursor.Error("Unterminated unicode escape");
                var c = cursor.Consume();
                var digit = HexDigit(c);
                if (digit < 0)
                    throw cursor.Error($"Invalid unicode escape digit '{c}'");
                code = code * 16 + digit;
            }

            return (char)code;
        }

        private static int HexDigit(char c)
        {
            if (c >= '0' && c <= '9')
                return c - '0';
            if (c >= 'a' && c <= 'f')
                return c - 'a' + 10;
            if (c >= 'A' && c <= 'F')
                return c - 'A' + 10;
            return -1;
        }

        private static JsonValue ParseNumber(Cursor cursor)
        {
            var start = cursor.Position;
            if (cursor.Peek() == '-')
                cursor.Consume();
            var digits = 0;
            while (!cursor.AtEnd && cursor.PeekRaw() >= '0' && cursor.PeekRaw() <= '9')
            {
                cursor.Consume();
                digits++;
            }

            if (digits == 0)
                throw cursor.Error("Invalid number");
            if (!cursor.AtEnd && cursor.PeekRaw() == '.')
            {
                cursor.Consume();
                var fraction = 0;
                while (!cursor.AtEnd && cursor.PeekRaw() >= '0' && cursor.PeekRaw() <= '9')
                {
                    cursor.Consume();
                    fraction++;
                }

                if (fraction == 0)
                    throw cursor.Error("Invalid number: missing fraction digits");
            }

            if (!cursor.AtEnd && (cursor.PeekRaw() == 'e' || cursor.PeekRaw() == 'E'))
            {
                cursor.Consume();
                if (!cursor.AtEnd && (cursor.PeekRaw() == '+' || cursor.PeekRaw() == '-'))
                    cursor.Consume();
                var exponent = 0;
                while (!cursor.AtEnd && cursor.PeekRaw() >= '0' && cursor.PeekRaw() <= '9')
                {
                    cursor.Consume();
                    exponent++;
                }

                if (exponent == 0)
                    throw cursor.Error("Invalid number: missing exponent digits");
            }

            var slice = cursor.Slice(start);
            var value = double.Parse(slice, NumberStyles.Float, CultureInfo.InvariantCulture);
            return new JsonValue { Kind = JsonKind.Number, Number = value };
        }

        private static JsonValue ParseLiteral(Cursor cursor, string literal, JsonValue value)
        {
            for (var i = 0; i < literal.Length; i++)
            {
                if (cursor.AtEnd || cursor.PeekRaw() != literal[i])
                    throw cursor.Error($"Invalid literal, expected '{literal}'");
                cursor.Consume();
            }

            return value;
        }

        private sealed class Cursor
        {
            private readonly string _text;
            private int _position;
            private int _line = 1;
            private int _lineStart;

            public Cursor(string text)
            {
                _text = text;
            }

            public bool AtEnd => _position >= _text.Length;

            public int Position => _position;

            public char Peek()
            {
                if (AtEnd)
                    throw Error("Unexpected end of input");
                return _text[_position];
            }

            public char PeekRaw() => _text[_position];

            public char Consume()
            {
                var c = _text[_position];
                _position++;
                if (c == '\n')
                {
                    _line++;
                    _lineStart = _position;
                }

                return c;
            }

            public void SkipWhitespace()
            {
                while (!AtEnd)
                {
                    var c = _text[_position];
                    if (c != ' ' && c != '\t' && c != '\n' && c != '\r')
                        return;
                    Consume();
                }
            }

            public string Slice(int start) => _text.Substring(start, _position - start);

            public JsonParseException Error(string message) => new JsonParseException(message, _line, _position - _lineStart + 1);
        }
    }
}

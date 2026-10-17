using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NodeGraphAvr.Editor
{
    public sealed class JsonWriter
    {
        private readonly StringBuilder _builder = new StringBuilder(1024);
        private readonly Stack<int> _itemCounts = new Stack<int>();

        public void BeginObject()
        {
            WriteItemPrefix();
            _builder.Append('{');
            _itemCounts.Push(0);
        }

        public void BeginObject(string name)
        {
            WritePropertyPrefix(name);
            _builder.Append('{');
            _itemCounts.Push(0);
        }

        public void EndObject()
        {
            CloseContainer('}');
        }

        public void BeginArray(string name)
        {
            WritePropertyPrefix(name);
            _builder.Append('[');
            _itemCounts.Push(0);
        }

        public void EndArray()
        {
            CloseContainer(']');
        }

        public void Property(string name, string value)
        {
            WritePropertyPrefix(name);
            WriteEscapedString(value);
        }

        public void Property(string name, long value)
        {
            WritePropertyPrefix(name);
            _builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        public void Property(string name, double value)
        {
            WritePropertyPrefix(name);
            _builder.Append(FormatDouble(value));
        }

        public void Property(string name, float value)
        {
            WritePropertyPrefix(name);
            _builder.Append(FormatFloat(value));
        }

        public void Property(string name, bool value)
        {
            WritePropertyPrefix(name);
            _builder.Append(value ? "true" : "false");
        }

        public void PropertyNull(string name)
        {
            WritePropertyPrefix(name);
            _builder.Append("null");
        }

        public void PropertyInlineInts(string name, int x, int y)
        {
            WritePropertyPrefix(name);
            _builder.Append('[');
            _builder.Append(x.ToString(CultureInfo.InvariantCulture));
            _builder.Append(", ");
            _builder.Append(y.ToString(CultureInfo.InvariantCulture));
            _builder.Append(']');
        }

        public void PropertyInlineInts(string name, int x, int y, int width, int height)
        {
            WritePropertyPrefix(name);
            _builder.Append('[');
            _builder.Append(x.ToString(CultureInfo.InvariantCulture));
            _builder.Append(", ");
            _builder.Append(y.ToString(CultureInfo.InvariantCulture));
            _builder.Append(", ");
            _builder.Append(width.ToString(CultureInfo.InvariantCulture));
            _builder.Append(", ");
            _builder.Append(height.ToString(CultureInfo.InvariantCulture));
            _builder.Append(']');
        }

        public void PropertyInlineFloats(string name, float[] components)
        {
            WritePropertyPrefix(name);
            _builder.Append('[');
            for (var i = 0; i < components.Length; i++)
            {
                if (i > 0)
                    _builder.Append(", ");
                _builder.Append(FormatFloat(components[i]));
            }

            _builder.Append(']');
        }

        public void ValueInlineInts(int x, int y)
        {
            WriteItemPrefix();
            _builder.Append('[');
            _builder.Append(x.ToString(CultureInfo.InvariantCulture));
            _builder.Append(", ");
            _builder.Append(y.ToString(CultureInfo.InvariantCulture));
            _builder.Append(']');
        }

        public void Value(string value)
        {
            WriteItemPrefix();
            WriteEscapedString(value);
        }

        public void Value(long value)
        {
            WriteItemPrefix();
            _builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        public void Value(double value)
        {
            WriteItemPrefix();
            _builder.Append(FormatDouble(value));
        }

        public void Value(float value)
        {
            WriteItemPrefix();
            _builder.Append(FormatFloat(value));
        }

        public void Value(bool value)
        {
            WriteItemPrefix();
            _builder.Append(value ? "true" : "false");
        }

        public void ValueNull()
        {
            WriteItemPrefix();
            _builder.Append("null");
        }

        public override string ToString() => _builder.ToString() + "\n";

        public static string FormatFloat(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        public static string FormatDouble(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private void WritePropertyPrefix(string name)
        {
            WriteItemPrefix();
            WriteEscapedString(name);
            _builder.Append(": ");
        }

        private void WriteItemPrefix()
        {
            if (_itemCounts.Count == 0)
                return;
            var count = _itemCounts.Pop();
            _itemCounts.Push(count + 1);
            _builder.Append(count > 0 ? ",\n" : "\n");
            _builder.Append(' ', _itemCounts.Count * 2);
        }

        private void CloseContainer(char closer)
        {
            var count = _itemCounts.Pop();
            if (count > 0)
            {
                _builder.Append('\n');
                _builder.Append(' ', _itemCounts.Count * 2);
            }

            _builder.Append(closer);
        }

        private void WriteEscapedString(string value)
        {
            _builder.Append('"');
            if (value != null)
            {
                for (var i = 0; i < value.Length; i++)
                {
                    var c = value[i];
                    switch (c)
                    {
                        case '"': _builder.Append("\\\""); break;
                        case '\\': _builder.Append("\\\\"); break;
                        case '\n': _builder.Append("\\n"); break;
                        case '\r': _builder.Append("\\r"); break;
                        case '\t': _builder.Append("\\t"); break;
                        case '\b': _builder.Append("\\b"); break;
                        case '\f': _builder.Append("\\f"); break;
                        default:
                            if (c < 0x20)
                                _builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            else
                                _builder.Append(c);
                            break;
                    }
                }
            }

            _builder.Append('"');
        }
    }
}

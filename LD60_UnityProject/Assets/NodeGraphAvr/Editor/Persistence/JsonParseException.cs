using System;

namespace NodeGraphAvr.Editor
{
    public sealed class JsonParseException : Exception
    {
        public int Line { get; }
        public int Column { get; }

        public JsonParseException(string message, int line, int column)
            : base($"{message} (line {line}, column {column})")
        {
            Line = line;
            Column = column;
        }
    }
}

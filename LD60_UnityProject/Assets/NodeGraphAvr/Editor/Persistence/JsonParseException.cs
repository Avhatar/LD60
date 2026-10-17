using System;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Raised by JsonParser on malformed input. Carries the 1-based line and column
    /// of the offending character so import errors point at the exact spot in the file.
    /// </summary>
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

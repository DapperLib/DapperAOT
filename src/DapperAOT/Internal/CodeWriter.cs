using System;
using System.Text;
using System.Threading;

namespace DapperAOT.Internal
{
    internal sealed class CodeWriter
    {
        static CodeWriter? s_Spare;
        public static CodeWriter Create()
            => Interlocked.Exchange(ref s_Spare, null) ?? new CodeWriter();

        private int _indent;
        public CodeWriter Clear() {
            sb.Clear();
            _indent = 0;
            return this;
        }
        private readonly StringBuilder sb = new StringBuilder();
        public CodeWriter Append(string? value)
        {
            sb.Append(value);
            return this;
        }
        public CodeWriter Append(char value)
        {
            sb.Append(value);
            return this;
        }
        internal CodeWriter Append(ReadOnlySpan<char> value)
        {
            sb.Append(value);
            return this;
        }

        public CodeWriter NewLine()
        {
            sb.AppendLine().Append('\t', _indent);
            return this;
        }
        public CodeWriter Indent(bool withScope = true)
        {
            if (withScope) NewLine().Append("{");
            _indent++;
            return this;
        }
        public CodeWriter Outdent(bool withScope = true)
        {
            _indent--;
            if (withScope) NewLine().Append("}");
            return this;
        }


        [Obsolete("You probably mean " + nameof(ToStringRecycle))]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        public override string ToString()
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
            => sb.ToString();
        public string ToStringRecycle()
        {
            var s = sb.ToString();
            Clear();
            Interlocked.Exchange(ref s_Spare, this);
            return s;
        }
    }
}

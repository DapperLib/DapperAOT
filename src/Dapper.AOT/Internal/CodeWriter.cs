using Microsoft.CodeAnalysis;
using System;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Dapper.Internal
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
        private readonly StringBuilder sb = new();

        public int Length
        {
            get => sb.Length;
            set => sb.Length = value;
        }

        public CodeWriter Append(string? value)
        {
            sb.Append(value);
            return this;
        }

        public CodeWriter Append(ITypeSymbol? value)
            => Append(value?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));


        public CodeWriter AppendVerbatimLiteral(string? value)
        {
            if (value is null) return Append("null");
            return Append("@\"").Append(value.Replace("\"", "\"\"")).Append("\"");
        }
        public CodeWriter Append(char value)
        {
            sb.Append(value);
            return this;
        }
        internal CodeWriter Append(ReadOnlySpan<char> value)
        {
            if (!value.IsEmpty)
            {
#if NETSTANDARD2_0
                unsafe
                {
                    fixed (char* ptr = value)
                    {
                        sb.Append(ptr, value.Length);
                    }
                }
#else
                sb.Append(value);
#endif
            }
            return this;
        }

        internal CodeWriter Append(int value)
        {
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
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

        public CodeWriter DisableWarning(string warning)
        {
            sb.AppendLine().Append("#pragma warning disable ").Append(warning);
            return this;
        }

        public CodeWriter RestoreWarning(string warning)
        {
            sb.AppendLine().Append("#pragma warning restore ").Append(warning);
            return this;
        }
        public CodeWriter DisableObsolete()
            => DisableWarning("CS0618");
        public CodeWriter RestoreObsolete()
            => RestoreWarning("CS0618");


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

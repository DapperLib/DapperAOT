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
        private bool _isLineEmpty = true;
        public CodeWriter Clear()
        {
            _sb.Clear();
            _indent = 0;
            _isLineEmpty = true;
            return this;
        }
        private readonly StringBuilder _sb = new();

        public int Length
        {
            get => _sb.Length;
            set => _sb.Length = value;
        }
        private StringBuilder Core
		{
            get
            {
                if (_isLineEmpty)
                {
                    _sb.Append('\t', _indent);
                    _isLineEmpty = false;
                }
                return _sb;
            }
		}

        public CodeWriter Append(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                Core.Append(value);
            }
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
            Core.Append(value);
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
                        Core.Append(ptr, value.Length);
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
            Core.Append(value.ToString(CultureInfo.InvariantCulture));
            return this;
        }

        public CodeWriter NewLine()
        {
            _sb.AppendLine();
            _isLineEmpty = true;
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
            NewLine();
            _sb.Append("#pragma warning disable ").Append(warning);
            return this;
        }

        public CodeWriter RestoreWarning(string warning)
        {
            NewLine();
            _sb.Append("#pragma warning restore ").Append(warning);
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
            => _sb.ToString();
        public string ToStringRecycle()
        {
            var s = _sb.ToString();
            Clear();
            Interlocked.Exchange(ref s_Spare, this);
            return s;
        }
    }
}

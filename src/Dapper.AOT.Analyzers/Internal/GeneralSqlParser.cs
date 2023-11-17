using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Dapper.Internal;

internal static class GeneralSqlParser
{
    private enum ParseState
    {
        None,
        Token,
        Variable,
        LineComment,
        BlockComment,
        String,
        Whitespace,
    }

    /// <summary>
    /// Tokenize a sql fragment into batches, extracting the variables/locals in use
    /// </summary>
    /// <remarks>This is a basic parse only; no syntax processing - just literals, identifiers, etc</remarks>
    public static List<CommandBatch> Parse(string sql, bool strip = false)
    {
        int bIndex = 0;
        char[] buffer = ArrayPool<char>.Shared.Rent(sql.Length + 1);

        char stringType = '\0';
        var state = ParseState.None;
        int i = 0, elementStartIndex = 0;
        ImmutableArray<string>.Builder? variables = null;
        var result = new List<CommandBatch>();
        bool LookBehind(char expected, int delta = 1)
            => Look(-delta, expected);

        bool LookAhead(char expected, int delta = 1)
            => Look(delta, expected);

        bool Look(int delta, char expected)
        {
            var ci = i + delta;
            return ci >= 0 && ci < sql.Length && sql[ci] == expected;
        }
        void Discard() => bIndex--;
        void NormalizeSpace()
        {
            if (strip)
            {
                if (bIndex > 1 && buffer[bIndex - 2] == ' ')
                {
                    Discard();
                }
                else
                {
                    buffer[bIndex - 1] = ' ';
                }
            }
        }
        bool ActivateStringPrefix()
        {
            if (i == elementStartIndex + 1)
            {
                stringType = sql[elementStartIndex];
                return true;
            };
            return false;
        }
        void SkipLeadingWhitespace(char v)
        {
            if (bIndex == 1 && ((v is '\r' or '\n') || strip))
            {
                // always omit leading CRLFs; omit leading whitespace
                // when stripping
                Discard();
            }
            else
            {
                NormalizeSpace();
            }
        }
        bool IsTrivialBatch()
        {
            if (bIndex < 2) return true;
            for (int i = 0; i < bIndex; i++)
            {
                if (char.IsWhiteSpace(buffer[i])
                    || (i == bIndex - 1 && buffer[i] == ';'))
                {
                    continue; // fine
                }
                return false;
            }
            return true;
        }

        bool IsString(char c) => state == ParseState.String && stringType == c;

        for (; i <= sql.Length; i++)
        {
            // store by default, we'll backtrack in the rare scenarios that we don't want it
            var c = i == sql.Length ? ';' : sql[i]; // spoof a ; at the end to simplify end-of-block handling
            buffer[bIndex++] = c;

            switch (state)
            {
                case ParseState.Whitespace when char.IsWhiteSpace(c): // more contiguous whitespace
                    if (strip) Discard();
                    else SkipLeadingWhitespace(c);
                    continue;
                case ParseState.LineComment when c is '\r' or '\n': // end of line comment
                    state = ParseState.Whitespace;
                    NormalizeSpace();
                    continue;
                case ParseState.BlockComment when c == '/' && LookBehind('*'): // end of block comment
                    NormalizeSpace();
                    state = ParseState.None;
                    continue;
                case ParseState.BlockComment or ParseState.LineComment: // keep ignoring line comment
                    if (strip) Discard();
                    continue;
                case ParseState.String when c == '\'' && IsString('E') && LookBehind('\\'): // E'...\'...'
                    continue;
                case ParseState.String when c == '\'' && (!IsString('"')):
                    if (!LookBehind('\'')) // [E]'...''...'
                    {
                        state = ParseState.None; // '.....'
                    }
                    continue;
                case ParseState.String when c == '"' && IsString('"'): // "....."
                    state = ParseState.None;
                    continue;
                case ParseState.String:
                    // ongoing string content
                    continue;
                case ParseState.Token when c == '\'' && ActivateStringPrefix(): // E'..., N'... etc
                    continue;
                case ParseState.Token or ParseState.Variable when IsToken(c):
                    // ongoing token / variable content
                    continue;
                case ParseState.Variable: // end of variable - store it
                    var name = sql.Substring(elementStartIndex, i - elementStartIndex);
                    variables ??= ImmutableArray.CreateBuilder<string>();
                    if (!variables.Contains(name)) variables.Add(name);
                    goto case ParseState.Token;
                case ParseState.Whitespace: // end of whitespace chunk
                case ParseState.Token: // end of token
                case ParseState.None: // not started
                    state = ParseState.None;
                    break; // make sure we still handle the value, below
                default:
                    throw new InvalidOperationException($"Token kind not handled: {state}");
            }


            if (c == '-' && LookAhead('-'))
            {
                state = ParseState.LineComment;
                if (strip) Discard();
                continue;
            }
            if (c == '/' && LookAhead('*'))
            {
                state = ParseState.BlockComment;
                if (strip) Discard();
                continue;
            }

            if (c == ';')
            {
                // end of batch
                if (strip)
                {
                    Discard(); // take any trailing whitespace, then re-attach
                    while (bIndex > 0 && char.IsWhiteSpace(buffer[bIndex - 1]))
                    {
                        bIndex--;
                    }
                    buffer[bIndex++] = ';';
                }

                if (!IsTrivialBatch())
                {
                    var batch = new string(buffer, 0, bIndex);
                    var args = variables is null ? ImmutableArray<string>.Empty : variables.ToImmutable();
                    result.Add(new(batch, args));
                }
                // logical reset
                bIndex = 0;
                variables?.Clear();
                state = ParseState.None;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                SkipLeadingWhitespace(c);
                state = ParseState.Whitespace;
                continue;
            }

            elementStartIndex = i;

            if (c is '"' or '\'')
            {
                // start a new string
                state = ParseState.String;
                stringType = c;
                continue;
            }

            if (SqlTools.ParameterPrefixCharacters.IndexOf(c) >= 0)
            {
                // start a new variable
                state = ParseState.Variable;
                continue;
            }

            if (IsToken(c))
            {
                // start a new token
                state = ParseState.Token;
                continue;
            }

            // other arbitrary syntax - operators etc
        }

        ArrayPool<char>.Shared.Return(buffer);
        return result;
        static bool IsToken(char c) => c == '_' || char.IsLetterOrDigit(c);
    }


    public readonly struct CommandBatch : IEquatable<CommandBatch>
    {
        public ImmutableArray<string> Variables { get; }
        public string Sql { get; }

        public CommandBatch(string sql) : this(sql, ImmutableArray<string>.Empty) { }
        public CommandBatch(string sql, string var0) : this(sql, ImmutableArray.Create(var0)) { }
        public CommandBatch(string sql, string var0, string var1) : this(sql, ImmutableArray.Create(var0, var1)) { }
        public CommandBatch(string sql, string var0, string var1, string var2) : this(sql, ImmutableArray.Create(var0, var1, var2)) { }
        public CommandBatch(string sql, string var0, string var1, string var2, string var3) : this(sql, ImmutableArray.Create(var0, var1, var2, var3)) { }
        public CommandBatch(string sql, string var0, string var1, string var2, string var3, string var4) : this(sql, ImmutableArray.Create(var0, var1, var2, var3, var4)) { }
        public CommandBatch(string sql, params string[] variables) : this(sql, ImmutableArray.Create(variables)) { }
        public CommandBatch(string sql, ImmutableArray<string> variables)
        {
            Sql = sql;
            Variables = variables;
        }

        public override int GetHashCode() => Sql.GetHashCode(); // args are a component of the sql; no need to hash them
        public override string ToString() => Variables.IsDefaultOrEmpty ? Sql :
            (Sql + " with " + string.Join(", ", Variables));

        public override bool Equals(object obj) => obj is CommandBatch other && Equals(other);

        public bool Equals(CommandBatch other)
            => Sql == other.Sql && Variables.SequenceEqual(other.Variables);
    }
}



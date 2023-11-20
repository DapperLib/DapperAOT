using Dapper.SqlAnalysis;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Dapper.Internal.SqlParsing;

internal readonly struct CommandVariable : IEquatable<CommandVariable>
{
    public CommandVariable(string name, int index)
    {
        Name = name;
        Index = index;
    }
    public int Index { get; }
    public string Name { get; }

    public ParameterKind Kind
    {
        get
        {
            var name = Name;
            if (name is { Length: >= 2 } && GeneralSqlParser.IsParameterPrefix(name[0]))
            {
                if ((char.IsLetter(name[1]) || name[1] == '_'))
                    return ParameterKind.Nominal;

                if (char.IsNumber(name[1]))
                    return ParameterKind.Ordinal;
            }

            return ParameterKind.Unknown;
        }
    }

    public override int GetHashCode() => Index;
    public override bool Equals(object obj) => obj is CommandVariable other && Equals(other);
    public bool Equals(CommandVariable other)
        => Index == other.Index && Name == other.Name;
    public override string ToString() => $"@{Index}:{Name}";
}
internal readonly struct CommandBatch : IEquatable<CommandBatch>
{
    public ImmutableArray<CommandVariable> Variables { get; }
    public string Sql { get; }

    public CommandBatch(string sql) : this(ImmutableArray<CommandVariable>.Empty, sql) { }
    public CommandBatch(string sql, CommandVariable var0) : this(ImmutableArray.Create(var0), sql) { }
    public CommandBatch(string sql, CommandVariable var0, CommandVariable var1) : this(ImmutableArray.Create(var0, var1), sql) { }
    public CommandBatch(string sql, CommandVariable var0, CommandVariable var1, CommandVariable var2) : this(ImmutableArray.Create(var0, var1, var2), sql) { }
    public CommandBatch(string sql, CommandVariable var0, CommandVariable var1, CommandVariable var2, CommandVariable var3) : this(ImmutableArray.Create(var0, var1, var2, var3), sql) { }
    public CommandBatch(string sql, CommandVariable var0, CommandVariable var1, CommandVariable var2, CommandVariable var3, CommandVariable var4) : this(ImmutableArray.Create(var0, var1, var2, var3, var4), sql) { }
    public static CommandBatch Create(string sql, params CommandVariable[] variables)
        => new(ImmutableArray.Create(variables), sql);
    public static CommandBatch Create(string sql, ImmutableArray<CommandVariable> variables)
        => new(variables, sql);
    // invert order to solve some .ctor ambiguity issues
    private CommandBatch(ImmutableArray<CommandVariable> variables, string sql)
    {
        Sql = sql;
        Variables = variables;
    }

    public ParameterKind ParameterKind
    {
        get
        {
            if (Variables.IsDefaultOrEmpty)
            {
                return ParameterKind.NonParametrized;
            }
            var first = Variables[0].Kind;
            foreach (var arg in Variables.AsSpan().Slice(1))
            {
                if (arg.Kind != first) return ParameterKind.Mixed;
            }
            return first;
        }
    }

    public override int GetHashCode() => Sql.GetHashCode(); // args are a component of the sql; no need to hash them
    public override string ToString() => Variables.IsDefaultOrEmpty ? Sql :
        (Sql + " with " + string.Join(", ", Variables));

    public override bool Equals(object obj) => obj is CommandBatch other && Equals(other);

    public bool Equals(CommandBatch other)
        => Sql == other.Sql && Variables.SequenceEqual(other.Variables);

    public OrdinalResult TryMakeOrdinal<T>(IList<T> inputArgs, Func<T, string> argName, Func<T, int, T> argFactory, out IList<T> args, out string sql, int argIndex = 0)
    {
        static bool TryFindByName(string name, IList<T> inputArgs, Func<T, string> argNameSelector, out T found)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2
                || !GeneralSqlParser.IsParameterPrefix(name[0]))
            {
                // general preconditions for nominal match: looking for @x
                // i.e. must have parameter symbol and at least one token character
                found = default!;
                return false;

            }
            foreach (var arg in inputArgs)
            {
                var argName = argNameSelector(arg);
                if (string.IsNullOrWhiteSpace(argName)) continue; // looking for nominal match

                // check for exact match including prefix, i.e. "@foo" vs "@foo"
                if (string.Equals(name, argName, StringComparison.OrdinalIgnoreCase))
                {
                    found = arg;
                    return true;
                }
                // check for input name excluding prefix, i.e. "foo" vs detected "@foo"
                // (when using Dapper, this is the normal usage)
                if (argName.Length == name.Length - 1 && !GeneralSqlParser.IsParameterPrefix(argName[0])
                    && name.EndsWith(argName, StringComparison.OrdinalIgnoreCase))
                {
                    found = arg;
                    return true;
                }
            }

            found = default!;
            return false;
        }
        sql = Sql;
        var kind = ParameterKind;
        switch (kind)
        {
            case ParameterKind.NonParametrized:
                args = [];
                return OrdinalResult.NoChange;
            case ParameterKind.Mixed:
                args = inputArgs;
                return OrdinalResult.MixedParameters;
            case ParameterKind.Ordinal:
                // TODO: rewrite, filtering and ordering; i.e.
                // where Id = $4 and Name = $3 -- no mention of $1 or $2
                // could be
                // where Id = $1 and Name = $2
                args = inputArgs;
                return OrdinalResult.NoChange;
            case ParameterKind.Nominal:
                break; // below
            default:
                args = inputArgs;
                return OrdinalResult.UnsupportedScenario;
        }

        Debug.Assert(kind == ParameterKind.Nominal);

        var map = new Dictionary<string, T>(Variables.Length, StringComparer.OrdinalIgnoreCase);
        args = new List<T>();
        var sb = new StringBuilder(sql);
        int delta = 0; // change in length of string
        foreach (var queryArg in Variables)
        {
            if (!map.TryGetValue(queryArg.Name, out var finalArg))
            {
                if (!TryFindByName(queryArg.Name, inputArgs, argName, out var found))
                {
                    args = inputArgs;
                    return OrdinalResult.UnsupportedScenario;
                }
                finalArg = argFactory(found, argIndex++);
                map.Add(queryArg.Name, finalArg);
                args.Add(finalArg);
            }
            var newName = argName(finalArg);
            // could potentially be more efficient with forwards-only write
            sb.Remove(queryArg.Index + delta, queryArg.Name.Length);
            sb.Insert(queryArg.Index + delta, newName);
            delta += newName.Length - queryArg.Name.Length;
        }
        sql = sb.ToString();
        return sql == Sql ? OrdinalResult.NoChange : OrdinalResult.Success;
    }

    internal static Func<string, int, string> OrdinalNaming { get; } = (name, index) => $"${index + 1}";
}

internal enum ParameterKind
{
    NonParametrized,
    Mixed,
    Ordinal, // $1
    Nominal,  // @foo
    Unknown,
}
internal enum OrdinalResult
{
    NoChange,
    MixedParameters,
    Success,
    UnsupportedScenario,
}

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
    public static List<CommandBatch> Parse(string sql, SqlSyntax syntax, bool strip = false)
    {
        // this is a basic first pass; TODO: rewrite using a forwards seek approach, i.e.
        // "find first [@$:'"...] etc, copy backfill then search for end of that symbol and process
        // accordingly

        int bIndex = 0;
        char[] buffer = ArrayPool<char>.Shared.Rent(sql.Length + 1);

        char stringType = '\0';
        var state = ParseState.None;
        int i = 0, elementStartbIndex = 0;
        ImmutableArray<CommandVariable>.Builder? variables = null;
        var result = new List<CommandBatch>();

        bool BatchSemicolon() => syntax == SqlSyntax.PostgreSql;

        char LookAhead(int delta = 1)
        {
            var ci = i + delta;
            return ci >= 0 && ci < sql.Length ? sql[ci] : '\0';
        }
        char Last(int offset)
        {
            var ci = bIndex - (offset + 1);
            return ci >= 0 && ci < bIndex ? buffer[ci] : '\0';
        }
        char LookBehind(int delta = 1) => LookAhead(-delta);
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
            if (ElementLength() == 2) // already written, so: N'... E'... etc
            {
                stringType = Last(0);
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
            else if (strip && Last(0) == ';')
            {
                Discard(); // don't write whitespace after ;
            }
            else
            {
                NormalizeSpace();
            }
        }
        int ElementLength() => bIndex - elementStartbIndex + 1;

        void FlushBatch()
        {
            if (IsGo()) bIndex -= 2; // don't retain the GO

            //bool removedSemicolon = false;
            if ((strip || BatchSemicolon()) && Last(0) == ';')
            {
                Discard();
                //removedSemicolon = true;
            }

            if (strip) // remove trailing whitespace
            {
                while (bIndex > 0 && char.IsWhiteSpace(buffer[bIndex - 1]))
                {
                    bIndex--;
                }
            }

            if (!IsWhitespace()) // anything left?
            {
                //if (removedSemicolon)
                //{
                //    // reattach
                //    buffer[bIndex++] = ';';
                //}

                var batchSql = new string(buffer, 0, bIndex);
                var args = variables is null ? ImmutableArray<CommandVariable>.Empty : variables.ToImmutable();
                result.Add(CommandBatch.Create(batchSql, args));
            }
            // logical reset
            bIndex = 0;
            variables?.Clear();
            state = ParseState.None;

            // lose any same-line simple space between batches
            while (LookAhead() is ' ' or '\t')
            {
                i++; // same as Advance();Discard();
            }
        }
        bool IsWhitespace()
        {
            if (bIndex == 0) return true;
            for (int i = 0; i < bIndex; i++)
            {
                if (!char.IsWhiteSpace(buffer[i])) return false;
            }
            return true;
        }
        bool IsGo()
        {
            return syntax == SqlSyntax.SqlServer && ElementLength() == 2
                    && Last(1) is 'g' or 'G' && Last(0) is 'o' or 'O';
        }
        void FlushVariable()
        {
            int varLen = ElementLength(), varStart = bIndex - varLen;
            var name = new string(buffer, varStart, varLen);
            variables ??= ImmutableArray.CreateBuilder<CommandVariable>();
            variables.Add(new(name, varStart));
        }

        bool IsString(char c) => state == ParseState.String && stringType == c;

        bool IsSingleQuoteString() => state == ParseState.String && (stringType == '\'' || char.IsLetter(stringType));
        void Advance() => buffer[bIndex++] = sql[++i];

        for (; i < sql.Length; i++)
        {
            var c = i == sql.Length ? ';' : sql[i]; // spoof a ; at the end to simplify end-of-block handling

            // detect end of GO token
            if (state == ParseState.Token && !IsToken(c) && IsGo())
            {
                FlushBatch(); // and keep going
            }
            else if (state == ParseState.Variable && !IsToken(c))
            {
                FlushVariable();
            }

            // store by default, we'll backtrack in the rare scenarios that we don't want it
            buffer[bIndex++] = sql[i];

            switch (state)
            {
                case ParseState.Whitespace when char.IsWhiteSpace(c): // more contiguous whitespace
                    if (strip) Discard();
                    else SkipLeadingWhitespace(c);
                    continue;
                case ParseState.LineComment when c is '\r' or '\n': // end of line comment
                case ParseState.BlockComment when c == '/' && LookBehind() == '*': // end of block comment
                    if (strip) Discard();
                    else NormalizeSpace();
                    state = ParseState.Whitespace;
                    continue;
                case ParseState.BlockComment or ParseState.LineComment: // keep ignoring line comment
                    if (strip) Discard();
                    continue;
                // string-escape characters
                case ParseState.String when c == '\'' && IsSingleQuoteString() && LookAhead() == '\'': // [?]'...''...'
                case ParseState.String when c == '"' && IsString('"') && LookAhead() == '\"': // "...""..."
                case ParseState.String when c == '\\' && (IsString('E') || IsString('e')) && LookAhead() != '\0': // E'...\*...'
                case ParseState.String when c == ']' && IsString('[') && LookAhead() == ']': // [...]]...]
                    // escaped or double-quote; move forwards immediately
                    Advance();
                    continue;
                // end string
                case ParseState.String when c == '"' && IsString('"'): // "....."
                case ParseState.String when c == ']' && IsString('['): // [.....]
                case ParseState.String when c == '\'' && IsSingleQuoteString(): // [?]'....'
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
                case ParseState.Variable: // end of variable
                case ParseState.Whitespace: // end of whitespace chunk
                case ParseState.Token: // end of token
                case ParseState.None: // not started
                    state = ParseState.None;
                    break; // make sure we still handle the value, below
                default:
                    throw new InvalidOperationException($"Token kind not handled: {state}");
            }

            if (c == '-' && LookAhead() == '-')
            {
                state = ParseState.LineComment;
                if (strip) Discard();
                continue;
            }
            if (c == '/' && LookAhead() == '*')
            {
                state = ParseState.BlockComment;
                if (strip) Discard();
                continue;
            }

            if (c == ';')
            {
                if (BatchSemicolon())
                {
                    FlushBatch();
                    continue;
                }

                // otherwise end-statement
                // (prevent unnecessary additional whitespace when stripping)
                state = ParseState.Whitespace;
                if (strip && Last(1) == ';')
                {   // squash down to just one
                    Discard();
                }
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                SkipLeadingWhitespace(c);
                state = ParseState.Whitespace;
                continue;
            }

            elementStartbIndex = bIndex;

            if (c is '"' or '\'' or '[')
            {
                // start a new string
                state = ParseState.String;
                stringType = c;
                continue;
            }

            if (IsParameterPrefix(c)
                && IsToken(LookAhead()) && LookBehind() != c) // avoid @>, @@IDENTTIY etc
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

        // deal with any remaining bits
        if (state == ParseState.Variable) FlushVariable();
        if (BatchSemicolon())
        {
            // spoof a final ;
            buffer[bIndex++] = ';';
        }
        FlushBatch();

        ArrayPool<char>.Shared.Return(buffer);
        return result;

        bool IsToken(char c) => c == '_' || char.IsLetterOrDigit(c)
            // npgsql allows @a.b as a valid parameter name
            || (c == '.' && state == ParseState.Variable && syntax == SqlSyntax.PostgreSql);
    }

    internal static bool IsParameterPrefix(char c)
        => SqlTools.ParameterPrefixCharacters.IndexOf(c) >= 0;
}



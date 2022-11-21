using System;
using System.Buffers;
using System.Data.Common;

namespace Dapper;

/// <summary>
/// Parses data of a given type
/// </summary>
public abstract class TypeReader
{
    /// <summary>
    /// Gets the primary token associated with the given <paramref name="columnName"/>, or <c>-1</c> if not found; the primary token
    /// assumes the exact data type and nullability, without flexible handling
    /// </summary>
    public virtual int GetToken(string columnName) => -1;
    /// <summary>
    /// Gets the more specific token associated with a primary token, for handling the discovered
    /// type and nullability - or <paramref name="token"/> if no better mamtch is available
    /// </summary>
    public virtual int GetToken(int token, Type type, bool isNullable) => token;

    /// <summary>
    /// Gets the anticipted type associated with this reader
    /// </summary>
    public abstract Type Type { get; }

    /// <summary>
    /// Identify the column tokens (<paramref name="tokens"/>) associated with a given <paramref name="dataReader"/>,
    /// starting at <paramref name="offset"/>
    /// </summary>
    public void IdentifyColumnTokens(DbDataReader dataReader, Span<int> tokens, int offset = 0)
    {
        for (int i = 0; i < tokens.Length; i++)
        {
            tokens[i] = GetToken(dataReader.GetName(offset + i));
        }
    }

    /// <summary>
    /// Read a single row of type <typeparamref name="T"/>, reading the columns specified via
    /// <paramref name="tokens"/>, with relative offset <paramref name="columnOffset"/>;
    /// so: if <paramref name="tokens"/> has length <c>3</c> and <paramref name="columnOffset"/> is
    /// <c>4</c>, then columns <c>4</c>, <c>5</c> and <c>6</c> from <paramref name="reader"/> should
    /// be consumed, using the definitions from <paramref name="tokens"/>
    /// </summary>
    public abstract object ReadObject(DbDataReader reader, ReadOnlySpan<int> tokens, int columnOffset = 0);

    /// <summary>
    /// Gets a right-sized buffer for the given size, liasing with the array-pool as necessary
    /// </summary>
    public static Span<int> RentSpan(ref int[]? buffer, int length)
    {
        if (buffer is object)
        {
            if (buffer.Length <= length)
                return new Span<int>(buffer, 0, length);

            // otherwise, existing buffer isn't big enough; return it
            // and we'll get a bigger one in a moment
            ArrayPool<int>.Shared.Return(buffer);
        }
        buffer = ArrayPool<int>.Shared.Rent(length);
        return new Span<int>(buffer, 0, length);
    }

    /// <summary>
    /// Return a buffer to the array-pool
    /// </summary>
    public static void Return(ref int[]? buffer)
    {
        if (buffer is not null)
        {
            ArrayPool<int>.Shared.Return(buffer);
            buffer = null;
        }
    }
}
/// <summary>
/// Parses data of type <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">The data type to be parsed</typeparam>
public abstract class TypeReader<T> : TypeReader
{
    /// <summary>
    /// Read a single row of type <typeparamref name="T"/>, reading the columns specified via
    /// <paramref name="tokens"/>, with relative offset <paramref name="columnOffset"/>;
    /// so: if <paramref name="tokens"/> has length <c>3</c> and <paramref name="columnOffset"/> is
    /// <c>4</c>, then columns <c>4</c>, <c>5</c> and <c>6</c> from <paramref name="reader"/> should
    /// be consumed, using the definitions from <paramref name="tokens"/>
    /// </summary>
    public abstract T Read(DbDataReader reader, ReadOnlySpan<int> tokens, int columnOffset = 0);

    /// <inheritdoc/>
    public sealed override object ReadObject(DbDataReader reader, ReadOnlySpan<int> tokens, int columnOffset = 0)
        => (T)Read(reader, tokens, columnOffset)!;

    /// <inheritdoc/>
    public sealed override Type Type => typeof(T);

    /// <summary>The recommended upper-bound on stack-allocated tokens</summary>
    public const int MaxStackTokens = 32;

    /// <summary>Reads a single row of type <typeparamref name="T"/></summary>
    public T Read(DbDataReader dataReader, ref int[]? tokenBuffer)
    {
        Span<int> tokens = dataReader.FieldCount <= MaxStackTokens ? stackalloc int[dataReader.FieldCount] : RentSpan(ref tokenBuffer, dataReader.FieldCount);
        IdentifyColumnTokens(dataReader, tokens);
        return Read(dataReader, tokens);
    }
}

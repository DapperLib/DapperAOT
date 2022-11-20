using System;
using System.Data.Common;

namespace Dapper;

/// <summary>
/// Parses data of a given type
/// </summary>
public interface ITypeReader
{
    /// <summary>
    /// Gets the primary token associated with the given <paramref name="columnName"/>, or <c>-1</c> if not found; the primary token
    /// assumes the exact data type and nullability, without flexible handling
    /// </summary>
    int GetToken(string columnName);
    /// <summary>
    /// Gets the more specific token associated with a primary token, for handling the discovered
    /// type and nullability - or <paramref name="token"/> if no better mamtch is available
    /// </summary>
    int GetToken(int token, Type type, bool isNullable);

    /// <summary>
    /// Gets the anticipted type associated with this reader
    /// </summary>
    Type Type { get; }
}
/// <summary>
/// Parses data of type <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">The data type to be parsed</typeparam>
public interface ITypeReader<T> : ITypeReader
{
    /// <summary>
    /// Read a row of type <typeparamref name="T"/>, reading the columns specified via
    /// <paramref name="tokens"/>, with relative offset <paramref name="columnOffset"/>;
    /// so: if <paramref name="tokens"/> has length <c>3</c> and <paramref name="columnOffset"/> is
    /// <c>4</c>, then columns <c>4</c>, <c>5</c> and <c>6</c> from <paramref name="reader"/> should
    /// be consumed, using the definitions from <paramref name="tokens"/>
    /// </summary>
    T Read(DbDataReader reader, ReadOnlySpan<int> tokens, int columnOffset = 0);
}

﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper;

/// <summary>
/// Describes a self-contained command, where there are either no parameters or the parameters are included in the setup.
/// </summary>
public readonly partial struct Command
{
    private readonly Command<ParameterBag> core;
    private readonly ParameterBag parameters;

    internal Parameter[] GetParameters() => parameters.ToArray();

    /// <inheritdoc/>
    public override string ToString() => core.ToString();

    /// <inheritdoc/>
    public override int GetHashCode() => core.GetHashCode();

    /// <inheritdoc/>
    public override bool Equals(object? obj) => core.Equals(obj);


    internal Command(DbConnection? connection, DbTransaction? transaction, string sql, CommandType commandType, int timeout, scoped ReadOnlySpan<Parameter> parameters)
    {
        core = new(connection, transaction, sql, commandType, timeout, ParameterBag.CommandFactory);
        this.parameters = ParameterBag.Create(parameters);
    }

    internal Command(DbConnection? connection, DbTransaction? transaction, string sql, CommandType commandType, int timeout, Parameter[]? parameters)
    {
        core = new(connection, transaction, sql, commandType, timeout, ParameterBag.CommandFactory);
        this.parameters = ParameterBag.Create(parameters);
    }

    /// <inheritdoc cref="Command{TArgs}.Execute(TArgs)"/>
    public int Execute() => core.Execute(parameters);

    /// <inheritdoc cref="Command{TArgs}.ExecuteAsync(TArgs, CancellationToken)"/>
    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default) => core.ExecuteAsync(parameters, cancellationToken);

    /// <inheritdoc cref="Command{TArgs}.Query{TRow}(TArgs, bool, RowFactory{TRow}?)"/>
    public IEnumerable<TRow> Query<TRow>(bool buffered, RowFactory<TRow>? rowFactory = null) => core.Query<TRow>(parameters, buffered, rowFactory);

    /// <inheritdoc cref="Command{TArgs}.QueryBuffered{TRow}(TArgs, RowFactory{TRow}?, int)"/>
    public List<TRow> QueryBuffered<TRow>(RowFactory<TRow>? rowFactory = null, int rowHintCount = 0) => core.QueryBuffered<TRow>(parameters, rowFactory, rowHintCount);

    /// <inheritdoc cref="Command{TArgs}.QueryUnbuffered{TRow}(TArgs, RowFactory{TRow}?)"/>
    public IEnumerable<TRow> QueryUnbuffered<TRow>(RowFactory<TRow>? rowFactory = null) => core.QueryUnbuffered<TRow>(parameters, rowFactory);

    /// <summary>
    /// A parameter defined during SQL composition.
    /// </summary>
    public readonly struct Parameter(string name, object value, DbType dbType, int size = 0)
        : IEquatable<Parameter>
    {
        internal bool IsAutoGenerated { get; }

        internal Parameter(string name, object value, DbType dbType, int size, bool isAutoGenerated)
            : this(name, value, dbType, size)
        {
            IsAutoGenerated = true; // used when testing parameter names; could use a [Flags] later
        }

        /// <summary>Create a new instance.</summary>
        public Parameter(string name, string value, int size = 0) : this(name, value, System.Data.DbType.String, size) { }

        /// <summary>Create a new instance.</summary>
        public Parameter(string name, int value) : this(name, CommandFactory.AsValue(value), System.Data.DbType.Int32) { }

        /// <summary>Create a new instance.</summary>
        public Parameter(string name, bool value) : this(name, CommandFactory.AsValue(value), System.Data.DbType.Boolean) { }

        internal const DbType UnknownType = (DbType)(-1);

        /// <inheritdoc/>
        public override bool Equals(
#if NET6_0_OR_GREATER
            [NotNullWhen(true)]
#endif
            object? obj)
            => obj is Parameter other && Equals(other);

        /// <inheritdoc/>
        public bool Equals(Parameter other)
            => other.Name == this.Name
            && Equals(other.Value, this.Value)
            && other.Size == this.Size
            && other.dbType == this.dbType;

        /// <inheritdoc/>
        public override int GetHashCode() => Name is null ? 0 : Name.GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => $"{Name}={Value} ({dbType}:{Size})";

        internal void AddParameter(in UnifiedCommand command)
        {
            var p = command.CreateParameter();
            p.ParameterName = Name;
            p.Value = Value;
            p.Direction = ParameterDirection.Input;
            if (Size != 0) p.Size = Size;
            if (dbType != UnknownType) p.DbType = dbType;
        }

        /// <summary>
        /// The name of this parameter.
        /// </summary>
        public string Name { get; } = name;

        /// <summary>
        /// The value of this parameter.
        /// </summary>
        public object Value { get; } = value;

        /// <summary>
        /// The size of this parameter as defined by <see cref="DbParameter.Size"/>.
        /// </summary>
        public int Size { get; } = size;

        private readonly DbType dbType = dbType;

        /// <summary>
        /// The type of this parameter as defined by <see cref="DbParameter.DbType"/>.
        /// </summary>
        public DbType? DbType => dbType == UnknownType ? null : dbType;
    }
}

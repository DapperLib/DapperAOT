// inspired from https://github.com/aspnet/Benchmarks/blob/main/src/Benchmarks/Data/DapperDb.cs
// in the case of any copyright conflict vs the core project, the following license applies to this file,
// to retain compatibility with the origin

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Dapper.Samples.DapperDbBenchmark
{
    public partial class DapperDb : IDb
    {
        private static readonly Comparison<World> WorldSortComparison = (a, b) => a.Id.CompareTo(b.Id);

        private readonly Random _random;
        [Connection]
        private readonly DbProviderFactory _dbProviderFactory;
        [ConnectionString]
        private readonly string _connectionString;

        public DapperDb(Random? random, DbProviderFactory dbProviderFactory, string connectionString)
        {
            _random = random ?? new Random();
            _dbProviderFactory = dbProviderFactory;
            _connectionString = connectionString;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int NextRandom() => _random.Next(1, 10001);

        public async Task<IEnumerable<Fortune>> LoadFortunesRows()
        {
            List<Fortune> result = await ReadFortunesRows();
            result.Add(new Fortune { Message = "Additional fortune added at request time." });
            result.Sort();

            return result;
        }

        public async Task<World[]> LoadMultipleQueriesRows(int count)
        {
            var results = new World[count];
            using var db = _dbProviderFactory.CreateConnection()!;
            db.ConnectionString = _connectionString;
            await db.OpenAsync();

            for (int i = 0; i < count; i++)
            {
                results[i] = await ReadSingleRow(NextRandom(), db);
            }

            return results;
        }

        public async Task<World[]> LoadMultipleUpdatesRows(int count)
        {
            var parameters = new Dictionary<string, int>(2 * count);

            using var db = _dbProviderFactory.CreateConnection()!;
            db.ConnectionString = _connectionString;
            await db.OpenAsync();

            var results = new World[count];
            for (int i = 0; i < count; i++)
            {
                results[i] = await ReadSingleRow(NextRandom(), db);
            }

            for (int i = 0; i < count; i++)
            {
                var randomNumber = NextRandom();
                parameters[$"@Random_{i}"] = randomNumber;
                parameters[$"@Id_{i}"] = results[i].Id;

                results[i].RandomNumber = randomNumber;
            }

            await ExecuteBatch(BatchUpdateString.Query(count), parameters, db);
            return results;
        }

        public Task<World> LoadSingleQueryRow() => ReadSingleRow(NextRandom());

        [Command("SELECT id, randomnumber FROM world WHERE id = @id", Annotate = false)]
        private partial Task<World> ReadSingleRow(int id, DbConnection? db = null);

        [Command("SELECT id, message FROM fortune", Annotate = false)]
        private partial Task<List<Fortune>> ReadFortunesRows();

        [Command(Annotate = false)]
        private partial Task ExecuteBatch([Command] string command, Dictionary<string, int> parameters, DbConnection? db);
    }

    public enum DatabaseServer
    {
        None,
        PostgreSql,
        SqlServer,
        MySql,
        MongoDb,
        Sqlite,
    }

    static class BatchUpdateString
    {
        private const int MaxBatch = 500;

        private static string[] _queries = new string[MaxBatch + 1];

        public static DatabaseServer DatabaseServer;

        public static string Query(int batchSize)
        {
            if (_queries[batchSize] != null)
            {
                return _queries[batchSize];
            }

            var lastIndex = batchSize - 1;

            var sb = new StringBuilder(); // StringBuilderCache.Acquire();

            if (DatabaseServer == DatabaseServer.PostgreSql)
            {
                sb.Append("UPDATE world SET randomNumber = temp.randomNumber FROM (VALUES ");
                Enumerable.Range(0, lastIndex).ToList().ForEach(i => sb.Append($"(@Id_{i}, @Random_{i}), "));
                sb.Append($"(@Id_{lastIndex}, @Random_{lastIndex}) ORDER BY 1) AS temp(id, randomNumber) WHERE temp.id = world.id");
            }
            else
            {
                Enumerable.Range(0, batchSize).ToList().ForEach(i => sb.Append($"UPDATE world SET randomnumber = @Random_{i} WHERE id = @Id_{i};"));
            }

            return _queries[batchSize] = sb.ToString(); // StringBuilderCache.GetStringAndRelease(sb);
        }
    }

    public interface IDb
    {
        Task<World> LoadSingleQueryRow();

        Task<World[]> LoadMultipleQueriesRows(int count);

        Task<World[]> LoadMultipleUpdatesRows(int count);

        Task<IEnumerable<Fortune>> LoadFortunesRows();
    }

    [Table("world")]
    public class World
    {
        [Column("id")]
        public int Id { get; set; }

        [IgnoreDataMember]
        [NotMapped]
        public int _Id { get; set; }

        [Column("randomnumber")]
        public int RandomNumber { get; set; }
    }

    [Table("fortune")]
    public class Fortune : IComparable<Fortune>, IComparable
    {
        [Column("id")]
        public int Id { get; set; }

        [NotMapped]
        [IgnoreDataMember]
        public int _Id { get; set; }

        [Column("message")]
        [StringLength(2048)]
        [IgnoreDataMember]
        [Required]
        public string? Message { get; set; }

        public int CompareTo(object? obj)
        {
            return CompareTo((Fortune?)obj);
        }

        public int CompareTo(Fortune? other) => string.CompareOrdinal(Message, other?.Message);
    }
}
#if !NETCOREAPP3_1_OR_GREATER
namespace System.ComponentModel.DataAnnotations
{
    sealed class RequiredAttribute : Attribute {}
    sealed class StringLengthAttribute : Attribute
    {
        public StringLengthAttribute(int _) {}
    }
}
namespace System.ComponentModel.DataAnnotations.Schema
{
    sealed class TableAttribute : Attribute
    {
        public TableAttribute(string name) {}
    }
    sealed class ColumnAttribute : Attribute
    {
        public ColumnAttribute(string name) {}
    }
    sealed class NotMappedAttribute : Attribute {}
}
#endif
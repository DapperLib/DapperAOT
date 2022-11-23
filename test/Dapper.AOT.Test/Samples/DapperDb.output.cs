
#nullable enable
//------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by:
//     Dapper.CodeAnalysis.CommandGenerator vN/A
// Changes to this file may cause incorrect behavior and
// will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#region Designer generated code
namespace Dapper.Samples.DapperDbBenchmark
{
	partial class DapperDb
	{

		[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
		[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
		private async partial global::System.Threading.Tasks.Task<global::Dapper.Samples.DapperDbBenchmark.World> ReadSingleRow(int id, global::System.Data.Common.DbConnection? db)
		{
			// locals
			global::System.Data.Common.DbCommand? __dapper__command = null;
			global::System.Data.Common.DbDataReader? __dapper__reader = null;
			bool __dapper__close = false, __dapper__dispose = false;
			int[]? __dapper__tokenBuffer = null;
			global::System.Data.Common.DbConnection? __dapper__connection = null;
			try
			{
				// prepare connection
				__dapper__connection = db;
				if (__dapper__connection is null)
				{
					__dapper__connection = _dbProviderFactory.CreateConnection();
					__dapper__connection!.ConnectionString = _connectionString;
					__dapper__dispose = true;
				}
				if (__dapper__connection!.State == global::System.Data.ConnectionState.Closed)
				{
					await __dapper__connection!.OpenAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false);
					__dapper__close = true;
				}

				// prepare command (excluding parameter values)
				__dapper__command = __dapper__CreateCommand(__dapper__connection!);

				// assign parameter values
				var __dapper__args = __dapper__command.Parameters;
#pragma warning disable CS0618
				__dapper__args[0].Value = global::Dapper.Internal.InternalUtilities.AsValue(id);
#pragma warning restore CS0618

				// execute reader
				const global::System.Data.CommandBehavior __dapper__behavior = global::System.Data.CommandBehavior.SequentialAccess | global::System.Data.CommandBehavior.SingleResult | global::System.Data.CommandBehavior.SingleRow;
				__dapper__reader = await __dapper__command.ExecuteReaderAsync(__dapper__close ? (__dapper__behavior | global::System.Data.CommandBehavior.CloseConnection) : __dapper__behavior, global::System.Threading.CancellationToken.None).ConfigureAwait(false);
				__dapper__close = false; // performed via CommandBehavior (if needed)

				// process single row
				global::Dapper.Samples.DapperDbBenchmark.World __dapper__result;
				if (__dapper__reader.HasRows && await __dapper__reader.ReadAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false))
				{
					__dapper__result = global::Dapper.Internal.__dapper__Run_TypeReaders.World.Instance.Read(__dapper__reader, ref __dapper__tokenBuffer);
				}
				else
				{
					__dapper__result = default!;
				}
				// consume additional results (ensures errors from the server are observed)
				while (await __dapper__reader.NextResultAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false)) { }
				return __dapper__result;

				// TODO: post-process parameters

			}
			finally
			{
				// cleanup
				global::Dapper.TypeReader.Return(ref __dapper__tokenBuffer);
				if (__dapper__reader is not null) await __dapper__reader.DisposeAsync().ConfigureAwait(false);
				if (__dapper__command is not null)
				{
					await __dapper__command.DisposeAsync().ConfigureAwait(false);
				}
				if (__dapper__connection is not null)
				{
					if (__dapper__close) await (__dapper__connection.CloseAsync() ?? global::System.Threading.Tasks.Task.CompletedTask).ConfigureAwait(false);
					if (__dapper__dispose) await __dapper__connection.DisposeAsync().ConfigureAwait(false);
				}
			}

			// command factory for ReadSingleRow
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
			[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
			static global::System.Data.Common.DbCommand __dapper__CreateCommand(global::System.Data.Common.DbConnection connection)
			{
				var command = connection.CreateCommand();
				if (command is global::Oracle.ManagedDataAccess.Client.OracleCommand typed0)
				{
					typed0.BindByName = true;
					typed0.InitialLONGFetchSize = -1;
				}
				command.CommandType = global::System.Data.CommandType.Text;
				command.CommandText = @"SELECT id, randomnumber FROM world WHERE id = @id";
				var args = command.Parameters;

				var p = command.CreateParameter();
				p.ParameterName = @"id";
				p.Direction = global::System.Data.ParameterDirection.Input;
				p.DbType = global::System.Data.DbType.Int32;
				args.Add(p);

				return command;
			}
		}


		[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
		[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
		private async partial global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Dapper.Samples.DapperDbBenchmark.Fortune>> ReadFortunesRows()
		{
			// locals
			global::System.Data.Common.DbCommand? __dapper__command = null;
			global::System.Data.Common.DbDataReader? __dapper__reader = null;
			bool __dapper__close = false, __dapper__dispose = false;
			int[]? __dapper__tokenBuffer = null;
			global::System.Collections.Generic.List<global::Dapper.Samples.DapperDbBenchmark.Fortune> __dapper__result;
			global::System.Data.Common.DbConnection? __dapper__connection = null;
			try
			{
				// prepare connection
				__dapper__connection = _dbProviderFactory.CreateConnection();
				__dapper__connection!.ConnectionString = _connectionString;
				__dapper__dispose = true;
				if (__dapper__connection!.State == global::System.Data.ConnectionState.Closed)
				{
					await __dapper__connection!.OpenAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false);
					__dapper__close = true;
				}

				// prepare command (excluding parameter values)
				__dapper__command = __dapper__CreateCommand(__dapper__connection!);

				// execute reader
				const global::System.Data.CommandBehavior __dapper__behavior = global::System.Data.CommandBehavior.SequentialAccess | global::System.Data.CommandBehavior.SingleResult;
				__dapper__reader = await __dapper__command.ExecuteReaderAsync(__dapper__close ? (__dapper__behavior | global::System.Data.CommandBehavior.CloseConnection) : __dapper__behavior, global::System.Threading.CancellationToken.None).ConfigureAwait(false);
				__dapper__close = false; // performed via CommandBehavior (if needed)

				// process multiple rows
				__dapper__result = new global::System.Collections.Generic.List<global::Dapper.Samples.DapperDbBenchmark.Fortune>();
				if (__dapper__reader.HasRows)
				{
					var __dapper__parser = global::Dapper.Internal.__dapper__Run_TypeReaders.Fortune.Instance;
					var __dapper__tokens = global::Dapper.TypeReader.RentSegment(ref __dapper__tokenBuffer, __dapper__reader.FieldCount);
					__dapper__parser.IdentifyColumnTokens(__dapper__reader, __dapper__tokens);
					while (await __dapper__reader.ReadAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false))
					{
						__dapper__result.Add(__dapper__parser.Read(__dapper__reader, __dapper__tokens));
					}
				}
				// consume additional results (ensures errors from the server are observed)
				while (await __dapper__reader.NextResultAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false)) { }

				// TODO: post-process parameters

				// return rowset
				return __dapper__result;
			}
			finally
			{
				// cleanup
				global::Dapper.TypeReader.Return(ref __dapper__tokenBuffer);
				if (__dapper__reader is not null) await __dapper__reader.DisposeAsync().ConfigureAwait(false);
				if (__dapper__command is not null)
				{
					await __dapper__command.DisposeAsync().ConfigureAwait(false);
				}
				if (__dapper__connection is not null)
				{
					if (__dapper__close) await (__dapper__connection.CloseAsync() ?? global::System.Threading.Tasks.Task.CompletedTask).ConfigureAwait(false);
					if (__dapper__dispose) await __dapper__connection.DisposeAsync().ConfigureAwait(false);
				}
			}

			// command factory for ReadFortunesRows
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
			[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
			static global::System.Data.Common.DbCommand __dapper__CreateCommand(global::System.Data.Common.DbConnection connection)
			{
				var command = connection.CreateCommand();
				if (command is global::Oracle.ManagedDataAccess.Client.OracleCommand typed0)
				{
					typed0.BindByName = true;
					typed0.InitialLONGFetchSize = -1;
				}
				command.CommandType = global::System.Data.CommandType.Text;
				command.CommandText = @"SELECT id, message FROM fortune";
				var args = command.Parameters;

				return command;
			}
		}


		[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
		[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
		private async partial global::System.Threading.Tasks.Task ExecuteBatch(string command, global::System.Collections.Generic.Dictionary<string, int> parameters, global::System.Data.Common.DbConnection? db)
		{
			// locals
			global::System.Data.Common.DbCommand? __dapper__command = null;
			bool __dapper__close = false, __dapper__dispose = false;
			global::System.Data.Common.DbConnection? __dapper__connection = null;
			try
			{
				// prepare connection
				__dapper__connection = db;
				if (__dapper__connection is null)
				{
					__dapper__connection = _dbProviderFactory.CreateConnection();
					__dapper__connection!.ConnectionString = _connectionString;
					__dapper__dispose = true;
				}
				if (__dapper__connection!.State == global::System.Data.ConnectionState.Closed)
				{
					await __dapper__connection!.OpenAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false);
					__dapper__close = true;
				}

				// prepare command (excluding parameter values)
				__dapper__command = __dapper__CreateCommand(__dapper__connection!, command);

				// assign parameter values
				var __dapper__args = __dapper__command.Parameters;
#pragma warning disable CS0618
				if (parameters is not null)
				{
					foreach (var __dapper__pair in parameters)
					{
						var __dapper__p = __dapper__command.CreateParameter();
						__dapper__p.ParameterName = __dapper__pair.Key;
						__dapper__p.Direction = global::System.Data.ParameterDirection.Input;
						__dapper__p.DbType = global::System.Data.DbType.Int32;
						__dapper__p.Value = global::Dapper.Internal.InternalUtilities.AsValue(__dapper__pair.Value);
						__dapper__args.Add(__dapper__p);
					}
				}
#pragma warning restore CS0618

				// execute non-query
				await __dapper__command.ExecuteNonQueryAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false);

				// TODO: post-process parameters

			}
			finally
			{
				// cleanup
				if (__dapper__command is not null)
				{
					await __dapper__command.DisposeAsync().ConfigureAwait(false);
				}
				if (__dapper__connection is not null)
				{
					if (__dapper__close) await (__dapper__connection.CloseAsync() ?? global::System.Threading.Tasks.Task.CompletedTask).ConfigureAwait(false);
					if (__dapper__dispose) await __dapper__connection.DisposeAsync().ConfigureAwait(false);
				}
			}

			// command factory for ExecuteBatch
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
			[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
			static global::System.Data.Common.DbCommand __dapper__CreateCommand(global::System.Data.Common.DbConnection connection, string? commandText)
			{
				var command = connection.CreateCommand();
				if (command is global::Oracle.ManagedDataAccess.Client.OracleCommand typed0)
				{
					typed0.BindByName = true;
					typed0.InitialLONGFetchSize = -1;
				}
				command.CommandType = global::System.Data.CommandType.Text;
				command.CommandText = commandText;
				var args = command.Parameters;

				return command;
			}
		}
	}
}

namespace Dapper.Internal.__dapper__Run_TypeReaders
{
	[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
	[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
	file sealed class Fortune : global::Dapper.TypeReader<global::Dapper.Samples.DapperDbBenchmark.Fortune>
	{
		private Fortune() { }
		public static readonly Fortune Instance = new();

		/// <inheritdoc/>
		public override int GetToken(string columnName)
		{
#pragma warning disable CS0618
			switch (global::Dapper.Internal.InternalUtilities.NormalizedHash(columnName))
			{
				case 619841764U:
					if (global::Dapper.Internal.InternalUtilities.NormalizedEquals(columnName, @"message")) return 1;
					break;
				case 926444256U:
					if (global::Dapper.Internal.InternalUtilities.NormalizedEquals(columnName, @"id")) return 0;
					break;
			}
#pragma warning restore CS0618
			return -1;
		}

		/// <inheritdoc/>
		public override int GetToken(int token, global::System.Type type, bool isNullable) => token;

		/// <inheritdoc/>
		public override global::Dapper.Samples.DapperDbBenchmark.Fortune Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset = 0)
		{
			global::Dapper.Samples.DapperDbBenchmark.Fortune obj = new();
			for (int i = 0; i < tokens.Length; i++)
			{
				switch (tokens[i])
				{
					case 0:
						obj.Id = reader.GetInt32(columnOffset + i);
						break;
					case 1 when !reader.IsDBNull(columnOffset + i):
						obj.Message = reader.GetString(columnOffset + i);
						break;
				}
			}
			return obj;
		}
	}
	[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
	[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
	file sealed class World : global::Dapper.TypeReader<global::Dapper.Samples.DapperDbBenchmark.World>
	{
		private World() { }
		public static readonly World Instance = new();

		/// <inheritdoc/>
		public override int GetToken(string columnName)
		{
#pragma warning disable CS0618
			switch (global::Dapper.Internal.InternalUtilities.NormalizedHash(columnName))
			{
				case 843736943U:
					if (global::Dapper.Internal.InternalUtilities.NormalizedEquals(columnName, @"randomnumber")) return 1;
					break;
				case 926444256U:
					if (global::Dapper.Internal.InternalUtilities.NormalizedEquals(columnName, @"id")) return 0;
					break;
			}
#pragma warning restore CS0618
			return -1;
		}

		/// <inheritdoc/>
		public override int GetToken(int token, global::System.Type type, bool isNullable) => token;

		/// <inheritdoc/>
		public override global::Dapper.Samples.DapperDbBenchmark.World Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset = 0)
		{
			global::Dapper.Samples.DapperDbBenchmark.World obj = new();
			for (int i = 0; i < tokens.Length; i++)
			{
				switch (tokens[i])
				{
					case 0:
						obj.Id = reader.GetInt32(columnOffset + i);
						break;
					case 1:
						obj.RandomNumber = reader.GetInt32(columnOffset + i);
						break;
				}
			}
			return obj;
		}
	}
}
#endregion
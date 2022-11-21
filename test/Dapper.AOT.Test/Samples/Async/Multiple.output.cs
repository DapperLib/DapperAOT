// Output code has 32 diagnostics from 'Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs':
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(61,54): error CS0117: 'TypeReader' does not contain a definition for 'TryGetReader'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(62,54): error CS0117: 'TypeReader' does not contain a definition for 'RentSegment'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(172,54): error CS0117: 'TypeReader' does not contain a definition for 'TryGetReader'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(173,54): error CS0117: 'TypeReader' does not contain a definition for 'RentSegment'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(282,54): error CS0117: 'TypeReader' does not contain a definition for 'TryGetReader'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(283,54): error CS0117: 'TypeReader' does not contain a definition for 'RentSegment'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(393,54): error CS0117: 'TypeReader' does not contain a definition for 'TryGetReader'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(394,54): error CS0117: 'TypeReader' does not contain a definition for 'RentSegment'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(503,24): error CS0118: 'Dapper.Internal.__dapper__Run_TypeReaders' is a namespace but is used like a variable
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(503,67): error CS0103: The name 'Instance' does not exist in the current context
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(611,54): error CS0117: 'TypeReader' does not contain a definition for 'TryGetReader'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(612,54): error CS0117: 'TypeReader' does not contain a definition for 'RentSegment'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(722,54): error CS0117: 'TypeReader' does not contain a definition for 'TryGetReader'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(723,54): error CS0117: 'TypeReader' does not contain a definition for 'RentSegment'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(834,54): error CS0117: 'TypeReader' does not contain a definition for 'TryGetReader'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(835,54): error CS0117: 'TypeReader' does not contain a definition for 'RentSegment'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(947,54): error CS0117: 'TypeReader' does not contain a definition for 'TryGetReader'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(948,54): error CS0117: 'TypeReader' does not contain a definition for 'RentSegment'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(1030,21): error CS0534: '' does not implement inherited abstract member 'TypeReader<SomeType[]>.Read(DbDataReader, ReadOnlySpan<int>, int)'
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(1030,21): error CS1001: Identifier expected
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(1031,3): error CS1513: } expected
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(1032,12): error CS8124: Tuple must contain at least two elements.
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(1032,14): error CS1022: Type or namespace definition, or end-of-file expected
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(1033,27): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(1033,36): error CS1525: Invalid expression term '='
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(1033,36): error CS8803: Top-level statements must precede namespace and type declarations.
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(1036,23): error CS0115: '<invalid-global-code>.GetToken(int, Type, bool)': no suitable method found to override
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(1036,23): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(1039,38): error CS0115: '<invalid-global-code>.Read(DbDataReader, ReadOnlySpan<int>, int)': no suitable method found to override
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(1041,29): error CS8752: The type 'SomeType[]' may not be used as the target type of new()
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(1044,2): error CS1022: Type or namespace definition, or end-of-file expected
// Dapper.AOT.Analyzers/Dapper.CodeAnalysis.CommandGenerator/Multiple.output.cs(1045,1): error CS1022: Type or namespace definition, or end-of-file expected

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
partial class Test
{

	// available inactive command for TaskAsync (interlocked)
	private static global::System.Data.Common.DbCommand? s___dapper__command_Samples_Async_Multiple_input_cs_TaskAsync_10;

	[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
	[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
	public async partial global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::SomeType>> TaskAsync(global::System.Data.Common.DbConnection connection, int id, string name)
	{
		// locals
		global::System.Data.Common.DbCommand? __dapper__command = null;
		global::System.Data.Common.DbDataReader? __dapper__reader = null;
		bool __dapper__close = false;
		int[]? __dapper__tokenBuffer = null;
		global::System.Collections.Generic.List<global::SomeType> __dapper__result;
		try
		{
			// prepare connection
			if (connection!.State == global::System.Data.ConnectionState.Closed)
			{
				await connection!.OpenAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false);
				__dapper__close = true;
			}

			// prepare command (excluding parameter values)
			if ((__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_TaskAsync_10, null)) is null)
			{
				__dapper__command = __dapper__CreateCommand(connection!);
			}
			else
			{
				__dapper__command.Connection = connection;
			}

			// assign parameter values
#pragma warning disable CS0618
			__dapper__command.Parameters[0].Value = global::Dapper.Internal.InternalUtilities.AsValue(id);
			__dapper__command.Parameters[1].Value = global::Dapper.Internal.InternalUtilities.AsValue(name);
#pragma warning restore CS0618

			// execute reader
			const global::System.Data.CommandBehavior __dapper__behavior = global::System.Data.CommandBehavior.SequentialAccess | global::System.Data.CommandBehavior.SingleResult | global::System.Data.CommandBehavior.SingleRow;
			__dapper__reader = await __dapper__command.ExecuteReaderAsync(__dapper__close ? (__dapper__behavior | global::System.Data.CommandBehavior.CloseConnection) : __dapper__behavior, global::System.Threading.CancellationToken.None).ConfigureAwait(false);
			__dapper__close = false; // performed via CommandBehavior

			// process multiple rows
			__dapper__result = new global::System.Collections.Generic.List<global::SomeType>();
			if (__dapper__reader.HasRows)
			{
				var __dapper__parser = global::Dapper.TypeReader.TryGetReader<global::SomeType>()!;
				var __dapper__tokens = global::Dapper.TypeReader.RentSegment(ref __dapper__tokenBuffer, __dapper__reader.FieldCount);
				__dapper__parser.IdentifyFieldTokensFromSchema(__dapper__reader, __dapper__tokens);
				while (await __dapper__reader.ReadAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false))
				{
					__dapper__result.Add(await __dapper__parser.ReadAsync(__dapper__reader, __dapper__tokens, global::System.Threading.CancellationToken.None).ConfigureAwait(false));
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
				__dapper__command.Connection = default;
				__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_TaskAsync_10, __dapper__command);
				if (__dapper__command is not null) await __dapper__command.DisposeAsync().ConfigureAwait(false);
			}
			if (__dapper__close) await (connection?.CloseAsync() ?? global::System.Threading.Tasks.Task.CompletedTask).ConfigureAwait(false);
		}

		// command factory for TaskAsync
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
			command.CommandType = global::System.Data.CommandType.StoredProcedure;
			command.CommandText = @"sproc";
			var args = command.Parameters;

			var p = command.CreateParameter();
			p.ParameterName = @"id";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.Int32;
			args.Add(p);

			p = command.CreateParameter();
			p.ParameterName = @"name";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.String;
			p.Size = -1;
			args.Add(p);

			return command;
		}
	}


	// available inactive command for ValueTaskAsync (interlocked)
	private static global::System.Data.Common.DbCommand? s___dapper__command_Samples_Async_Multiple_input_cs_ValueTaskAsync_13;

	[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
	[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
	[global::System.Runtime.CompilerServices.AsyncMethodBuilderAttribute(typeof(global::System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder<>))]
	public async partial global::System.Threading.Tasks.ValueTask<global::System.Collections.Generic.List<global::SomeType>> ValueTaskAsync(global::System.Data.Common.DbConnection connection, int id, string name)
	{
		// locals
		global::System.Data.Common.DbCommand? __dapper__command = null;
		global::System.Data.Common.DbDataReader? __dapper__reader = null;
		bool __dapper__close = false;
		int[]? __dapper__tokenBuffer = null;
		global::System.Collections.Generic.List<global::SomeType> __dapper__result;
		try
		{
			// prepare connection
			if (connection!.State == global::System.Data.ConnectionState.Closed)
			{
				await connection!.OpenAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false);
				__dapper__close = true;
			}

			// prepare command (excluding parameter values)
			if ((__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_ValueTaskAsync_13, null)) is null)
			{
				__dapper__command = __dapper__CreateCommand(connection!);
			}
			else
			{
				__dapper__command.Connection = connection;
			}

			// assign parameter values
#pragma warning disable CS0618
			__dapper__command.Parameters[0].Value = global::Dapper.Internal.InternalUtilities.AsValue(id);
			__dapper__command.Parameters[1].Value = global::Dapper.Internal.InternalUtilities.AsValue(name);
#pragma warning restore CS0618

			// execute reader
			const global::System.Data.CommandBehavior __dapper__behavior = global::System.Data.CommandBehavior.SequentialAccess | global::System.Data.CommandBehavior.SingleResult | global::System.Data.CommandBehavior.SingleRow;
			__dapper__reader = await __dapper__command.ExecuteReaderAsync(__dapper__close ? (__dapper__behavior | global::System.Data.CommandBehavior.CloseConnection) : __dapper__behavior, global::System.Threading.CancellationToken.None).ConfigureAwait(false);
			__dapper__close = false; // performed via CommandBehavior

			// process multiple rows
			__dapper__result = new global::System.Collections.Generic.List<global::SomeType>();
			if (__dapper__reader.HasRows)
			{
				var __dapper__parser = global::Dapper.TypeReader.TryGetReader<global::SomeType>()!;
				var __dapper__tokens = global::Dapper.TypeReader.RentSegment(ref __dapper__tokenBuffer, __dapper__reader.FieldCount);
				__dapper__parser.IdentifyFieldTokensFromSchema(__dapper__reader, __dapper__tokens);
				while (await __dapper__reader.ReadAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false))
				{
					__dapper__result.Add(await __dapper__parser.ReadAsync(__dapper__reader, __dapper__tokens, global::System.Threading.CancellationToken.None).ConfigureAwait(false));
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
				__dapper__command.Connection = default;
				__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_ValueTaskAsync_13, __dapper__command);
				if (__dapper__command is not null) await __dapper__command.DisposeAsync().ConfigureAwait(false);
			}
			if (__dapper__close) await (connection?.CloseAsync() ?? global::System.Threading.Tasks.Task.CompletedTask).ConfigureAwait(false);
		}

		// command factory for ValueTaskAsync
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
			command.CommandType = global::System.Data.CommandType.StoredProcedure;
			command.CommandText = @"sproc";
			var args = command.Parameters;

			var p = command.CreateParameter();
			p.ParameterName = @"id";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.Int32;
			args.Add(p);

			p = command.CreateParameter();
			p.ParameterName = @"name";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.String;
			p.Size = -1;
			args.Add(p);

			return command;
		}
	}


	// available inactive command for TaskWithCancellationAsync (interlocked)
	private static global::System.Data.Common.DbCommand? s___dapper__command_Samples_Async_Multiple_input_cs_TaskWithCancellationAsync_16;

	[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
	[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
	public async partial global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::SomeType>> TaskWithCancellationAsync(global::System.Data.Common.DbConnection connection, int id, string name, global::System.Threading.CancellationToken cancellation)
	{
		// locals
		global::System.Data.Common.DbCommand? __dapper__command = null;
		global::System.Data.Common.DbDataReader? __dapper__reader = null;
		bool __dapper__close = false;
		int[]? __dapper__tokenBuffer = null;
		global::System.Collections.Generic.List<global::SomeType> __dapper__result;
		try
		{
			// prepare connection
			if (connection!.State == global::System.Data.ConnectionState.Closed)
			{
				await connection!.OpenAsync(cancellation).ConfigureAwait(false);
				__dapper__close = true;
			}

			// prepare command (excluding parameter values)
			if ((__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_TaskWithCancellationAsync_16, null)) is null)
			{
				__dapper__command = __dapper__CreateCommand(connection!);
			}
			else
			{
				__dapper__command.Connection = connection;
			}

			// assign parameter values
#pragma warning disable CS0618
			__dapper__command.Parameters[0].Value = global::Dapper.Internal.InternalUtilities.AsValue(id);
			__dapper__command.Parameters[1].Value = global::Dapper.Internal.InternalUtilities.AsValue(name);
#pragma warning restore CS0618

			// execute reader
			const global::System.Data.CommandBehavior __dapper__behavior = global::System.Data.CommandBehavior.SequentialAccess | global::System.Data.CommandBehavior.SingleResult | global::System.Data.CommandBehavior.SingleRow;
			__dapper__reader = await __dapper__command.ExecuteReaderAsync(__dapper__close ? (__dapper__behavior | global::System.Data.CommandBehavior.CloseConnection) : __dapper__behavior, cancellation).ConfigureAwait(false);
			__dapper__close = false; // performed via CommandBehavior

			// process multiple rows
			__dapper__result = new global::System.Collections.Generic.List<global::SomeType>();
			if (__dapper__reader.HasRows)
			{
				var __dapper__parser = global::Dapper.TypeReader.TryGetReader<global::SomeType>()!;
				var __dapper__tokens = global::Dapper.TypeReader.RentSegment(ref __dapper__tokenBuffer, __dapper__reader.FieldCount);
				__dapper__parser.IdentifyFieldTokensFromSchema(__dapper__reader, __dapper__tokens);
				while (await __dapper__reader.ReadAsync(cancellation).ConfigureAwait(false))
				{
					__dapper__result.Add(await __dapper__parser.ReadAsync(__dapper__reader, __dapper__tokens, cancellation).ConfigureAwait(false));
				}
			}
			// consume additional results (ensures errors from the server are observed)
			while (await __dapper__reader.NextResultAsync(cancellation).ConfigureAwait(false)) { }

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
				__dapper__command.Connection = default;
				__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_TaskWithCancellationAsync_16, __dapper__command);
				if (__dapper__command is not null) await __dapper__command.DisposeAsync().ConfigureAwait(false);
			}
			if (__dapper__close) await (connection?.CloseAsync() ?? global::System.Threading.Tasks.Task.CompletedTask).ConfigureAwait(false);
		}

		// command factory for TaskWithCancellationAsync
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
			command.CommandType = global::System.Data.CommandType.StoredProcedure;
			command.CommandText = @"sproc";
			var args = command.Parameters;

			var p = command.CreateParameter();
			p.ParameterName = @"id";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.Int32;
			args.Add(p);

			p = command.CreateParameter();
			p.ParameterName = @"name";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.String;
			p.Size = -1;
			args.Add(p);

			return command;
		}
	}


	// available inactive command for ValueWithCancellationTaskAsync (interlocked)
	private static global::System.Data.Common.DbCommand? s___dapper__command_Samples_Async_Multiple_input_cs_ValueWithCancellationTaskAsync_19;

	[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
	[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
	[global::System.Runtime.CompilerServices.AsyncMethodBuilderAttribute(typeof(global::System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder<>))]
	public async partial global::System.Threading.Tasks.ValueTask<global::System.Collections.Generic.List<global::SomeType>> ValueWithCancellationTaskAsync(global::System.Data.Common.DbConnection connection, int id, string name, global::System.Threading.CancellationToken cancellation)
	{
		// locals
		global::System.Data.Common.DbCommand? __dapper__command = null;
		global::System.Data.Common.DbDataReader? __dapper__reader = null;
		bool __dapper__close = false;
		int[]? __dapper__tokenBuffer = null;
		global::System.Collections.Generic.List<global::SomeType> __dapper__result;
		try
		{
			// prepare connection
			if (connection!.State == global::System.Data.ConnectionState.Closed)
			{
				await connection!.OpenAsync(cancellation).ConfigureAwait(false);
				__dapper__close = true;
			}

			// prepare command (excluding parameter values)
			if ((__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_ValueWithCancellationTaskAsync_19, null)) is null)
			{
				__dapper__command = __dapper__CreateCommand(connection!);
			}
			else
			{
				__dapper__command.Connection = connection;
			}

			// assign parameter values
#pragma warning disable CS0618
			__dapper__command.Parameters[0].Value = global::Dapper.Internal.InternalUtilities.AsValue(id);
			__dapper__command.Parameters[1].Value = global::Dapper.Internal.InternalUtilities.AsValue(name);
#pragma warning restore CS0618

			// execute reader
			const global::System.Data.CommandBehavior __dapper__behavior = global::System.Data.CommandBehavior.SequentialAccess | global::System.Data.CommandBehavior.SingleResult | global::System.Data.CommandBehavior.SingleRow;
			__dapper__reader = await __dapper__command.ExecuteReaderAsync(__dapper__close ? (__dapper__behavior | global::System.Data.CommandBehavior.CloseConnection) : __dapper__behavior, cancellation).ConfigureAwait(false);
			__dapper__close = false; // performed via CommandBehavior

			// process multiple rows
			__dapper__result = new global::System.Collections.Generic.List<global::SomeType>();
			if (__dapper__reader.HasRows)
			{
				var __dapper__parser = global::Dapper.TypeReader.TryGetReader<global::SomeType>()!;
				var __dapper__tokens = global::Dapper.TypeReader.RentSegment(ref __dapper__tokenBuffer, __dapper__reader.FieldCount);
				__dapper__parser.IdentifyFieldTokensFromSchema(__dapper__reader, __dapper__tokens);
				while (await __dapper__reader.ReadAsync(cancellation).ConfigureAwait(false))
				{
					__dapper__result.Add(await __dapper__parser.ReadAsync(__dapper__reader, __dapper__tokens, cancellation).ConfigureAwait(false));
				}
			}
			// consume additional results (ensures errors from the server are observed)
			while (await __dapper__reader.NextResultAsync(cancellation).ConfigureAwait(false)) { }

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
				__dapper__command.Connection = default;
				__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_ValueWithCancellationTaskAsync_19, __dapper__command);
				if (__dapper__command is not null) await __dapper__command.DisposeAsync().ConfigureAwait(false);
			}
			if (__dapper__close) await (connection?.CloseAsync() ?? global::System.Threading.Tasks.Task.CompletedTask).ConfigureAwait(false);
		}

		// command factory for ValueWithCancellationTaskAsync
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
			command.CommandType = global::System.Data.CommandType.StoredProcedure;
			command.CommandText = @"sproc";
			var args = command.Parameters;

			var p = command.CreateParameter();
			p.ParameterName = @"id";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.Int32;
			args.Add(p);

			p = command.CreateParameter();
			p.ParameterName = @"name";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.String;
			p.Size = -1;
			args.Add(p);

			return command;
		}
	}


	// available inactive command for ArrayAsync (interlocked)
	private static global::System.Data.Common.DbCommand? s___dapper__command_Samples_Async_Multiple_input_cs_ArrayAsync_22;

	[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
	[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
	[global::System.Runtime.CompilerServices.AsyncMethodBuilderAttribute(typeof(global::System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder<>))]
	public async partial global::System.Threading.Tasks.ValueTask<global::SomeType[]> ArrayAsync(global::System.Data.Common.DbConnection connection, int id, string name)
	{
		// locals
		global::System.Data.Common.DbCommand? __dapper__command = null;
		global::System.Data.Common.DbDataReader? __dapper__reader = null;
		bool __dapper__close = false;
		int[]? __dapper__tokenBuffer = null;
		try
		{
			// prepare connection
			if (connection!.State == global::System.Data.ConnectionState.Closed)
			{
				await connection!.OpenAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false);
				__dapper__close = true;
			}

			// prepare command (excluding parameter values)
			if ((__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_ArrayAsync_22, null)) is null)
			{
				__dapper__command = __dapper__CreateCommand(connection!);
			}
			else
			{
				__dapper__command.Connection = connection;
			}

			// assign parameter values
#pragma warning disable CS0618
			__dapper__command.Parameters[0].Value = global::Dapper.Internal.InternalUtilities.AsValue(id);
			__dapper__command.Parameters[1].Value = global::Dapper.Internal.InternalUtilities.AsValue(name);
#pragma warning restore CS0618

			// execute reader
			const global::System.Data.CommandBehavior __dapper__behavior = global::System.Data.CommandBehavior.SequentialAccess | global::System.Data.CommandBehavior.SingleResult | global::System.Data.CommandBehavior.SingleRow;
			__dapper__reader = await __dapper__command.ExecuteReaderAsync(__dapper__close ? (__dapper__behavior | global::System.Data.CommandBehavior.CloseConnection) : __dapper__behavior, global::System.Threading.CancellationToken.None).ConfigureAwait(false);
			__dapper__close = false; // performed via CommandBehavior

			// process single row
			global::SomeType[] __dapper__result;
			if (__dapper__reader.HasRows && await __dapper__reader.ReadAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false))
			{
				__dapper__result = Dapper.Internal.__dapper__Run_TypeReaders..Instance.Read(__dapper__reader, ref __dapper__tokenBuffer);
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
				__dapper__command.Connection = default;
				__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_ArrayAsync_22, __dapper__command);
				if (__dapper__command is not null) await __dapper__command.DisposeAsync().ConfigureAwait(false);
			}
			if (__dapper__close) await (connection?.CloseAsync() ?? global::System.Threading.Tasks.Task.CompletedTask).ConfigureAwait(false);
		}

		// command factory for ArrayAsync
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
			command.CommandType = global::System.Data.CommandType.StoredProcedure;
			command.CommandText = @"sproc";
			var args = command.Parameters;

			var p = command.CreateParameter();
			p.ParameterName = @"id";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.Int32;
			args.Add(p);

			p = command.CreateParameter();
			p.ParameterName = @"name";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.String;
			p.Size = -1;
			args.Add(p);

			return command;
		}
	}


	// available inactive command for IListAsync (interlocked)
	private static global::System.Data.Common.DbCommand? s___dapper__command_Samples_Async_Multiple_input_cs_IListAsync_25;

	[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
	[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
	[global::System.Runtime.CompilerServices.AsyncMethodBuilderAttribute(typeof(global::System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder<>))]
	public async partial global::System.Threading.Tasks.ValueTask<global::System.Collections.Generic.IList<global::SomeType>> IListAsync(global::System.Data.Common.DbConnection connection, int id, string name)
	{
		// locals
		global::System.Data.Common.DbCommand? __dapper__command = null;
		global::System.Data.Common.DbDataReader? __dapper__reader = null;
		bool __dapper__close = false;
		int[]? __dapper__tokenBuffer = null;
		global::System.Collections.Generic.List<global::SomeType> __dapper__result;
		try
		{
			// prepare connection
			if (connection!.State == global::System.Data.ConnectionState.Closed)
			{
				await connection!.OpenAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false);
				__dapper__close = true;
			}

			// prepare command (excluding parameter values)
			if ((__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_IListAsync_25, null)) is null)
			{
				__dapper__command = __dapper__CreateCommand(connection!);
			}
			else
			{
				__dapper__command.Connection = connection;
			}

			// assign parameter values
#pragma warning disable CS0618
			__dapper__command.Parameters[0].Value = global::Dapper.Internal.InternalUtilities.AsValue(id);
			__dapper__command.Parameters[1].Value = global::Dapper.Internal.InternalUtilities.AsValue(name);
#pragma warning restore CS0618

			// execute reader
			const global::System.Data.CommandBehavior __dapper__behavior = global::System.Data.CommandBehavior.SequentialAccess | global::System.Data.CommandBehavior.SingleResult | global::System.Data.CommandBehavior.SingleRow;
			__dapper__reader = await __dapper__command.ExecuteReaderAsync(__dapper__close ? (__dapper__behavior | global::System.Data.CommandBehavior.CloseConnection) : __dapper__behavior, global::System.Threading.CancellationToken.None).ConfigureAwait(false);
			__dapper__close = false; // performed via CommandBehavior

			// process multiple rows
			__dapper__result = new global::System.Collections.Generic.List<global::SomeType>();
			if (__dapper__reader.HasRows)
			{
				var __dapper__parser = global::Dapper.TypeReader.TryGetReader<global::SomeType>()!;
				var __dapper__tokens = global::Dapper.TypeReader.RentSegment(ref __dapper__tokenBuffer, __dapper__reader.FieldCount);
				__dapper__parser.IdentifyFieldTokensFromSchema(__dapper__reader, __dapper__tokens);
				while (await __dapper__reader.ReadAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false))
				{
					__dapper__result.Add(await __dapper__parser.ReadAsync(__dapper__reader, __dapper__tokens, global::System.Threading.CancellationToken.None).ConfigureAwait(false));
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
				__dapper__command.Connection = default;
				__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_IListAsync_25, __dapper__command);
				if (__dapper__command is not null) await __dapper__command.DisposeAsync().ConfigureAwait(false);
			}
			if (__dapper__close) await (connection?.CloseAsync() ?? global::System.Threading.Tasks.Task.CompletedTask).ConfigureAwait(false);
		}

		// command factory for IListAsync
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
			command.CommandType = global::System.Data.CommandType.StoredProcedure;
			command.CommandText = @"sproc";
			var args = command.Parameters;

			var p = command.CreateParameter();
			p.ParameterName = @"id";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.Int32;
			args.Add(p);

			p = command.CreateParameter();
			p.ParameterName = @"name";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.String;
			p.Size = -1;
			args.Add(p);

			return command;
		}
	}


	// available inactive command for ICollectionAsync (interlocked)
	private static global::System.Data.Common.DbCommand? s___dapper__command_Samples_Async_Multiple_input_cs_ICollectionAsync_28;

	[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
	[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
	[global::System.Runtime.CompilerServices.AsyncMethodBuilderAttribute(typeof(global::System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder<>))]
	public async partial global::System.Threading.Tasks.ValueTask<global::System.Collections.Generic.ICollection<global::SomeType>> ICollectionAsync(global::System.Data.Common.DbConnection connection, int id, string name)
	{
		// locals
		global::System.Data.Common.DbCommand? __dapper__command = null;
		global::System.Data.Common.DbDataReader? __dapper__reader = null;
		bool __dapper__close = false;
		int[]? __dapper__tokenBuffer = null;
		global::System.Collections.Generic.List<global::SomeType> __dapper__result;
		try
		{
			// prepare connection
			if (connection!.State == global::System.Data.ConnectionState.Closed)
			{
				await connection!.OpenAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false);
				__dapper__close = true;
			}

			// prepare command (excluding parameter values)
			if ((__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_ICollectionAsync_28, null)) is null)
			{
				__dapper__command = __dapper__CreateCommand(connection!);
			}
			else
			{
				__dapper__command.Connection = connection;
			}

			// assign parameter values
#pragma warning disable CS0618
			__dapper__command.Parameters[0].Value = global::Dapper.Internal.InternalUtilities.AsValue(id);
			__dapper__command.Parameters[1].Value = global::Dapper.Internal.InternalUtilities.AsValue(name);
#pragma warning restore CS0618

			// execute reader
			const global::System.Data.CommandBehavior __dapper__behavior = global::System.Data.CommandBehavior.SequentialAccess | global::System.Data.CommandBehavior.SingleResult | global::System.Data.CommandBehavior.SingleRow;
			__dapper__reader = await __dapper__command.ExecuteReaderAsync(__dapper__close ? (__dapper__behavior | global::System.Data.CommandBehavior.CloseConnection) : __dapper__behavior, global::System.Threading.CancellationToken.None).ConfigureAwait(false);
			__dapper__close = false; // performed via CommandBehavior

			// process multiple rows
			__dapper__result = new global::System.Collections.Generic.List<global::SomeType>();
			if (__dapper__reader.HasRows)
			{
				var __dapper__parser = global::Dapper.TypeReader.TryGetReader<global::SomeType>()!;
				var __dapper__tokens = global::Dapper.TypeReader.RentSegment(ref __dapper__tokenBuffer, __dapper__reader.FieldCount);
				__dapper__parser.IdentifyFieldTokensFromSchema(__dapper__reader, __dapper__tokens);
				while (await __dapper__reader.ReadAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false))
				{
					__dapper__result.Add(await __dapper__parser.ReadAsync(__dapper__reader, __dapper__tokens, global::System.Threading.CancellationToken.None).ConfigureAwait(false));
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
				__dapper__command.Connection = default;
				__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_ICollectionAsync_28, __dapper__command);
				if (__dapper__command is not null) await __dapper__command.DisposeAsync().ConfigureAwait(false);
			}
			if (__dapper__close) await (connection?.CloseAsync() ?? global::System.Threading.Tasks.Task.CompletedTask).ConfigureAwait(false);
		}

		// command factory for ICollectionAsync
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
			command.CommandType = global::System.Data.CommandType.StoredProcedure;
			command.CommandText = @"sproc";
			var args = command.Parameters;

			var p = command.CreateParameter();
			p.ParameterName = @"id";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.Int32;
			args.Add(p);

			p = command.CreateParameter();
			p.ParameterName = @"name";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.String;
			p.Size = -1;
			args.Add(p);

			return command;
		}
	}


	// available inactive command for ImmutableArrayAsync (interlocked)
	private static global::System.Data.Common.DbCommand? s___dapper__command_Samples_Async_Multiple_input_cs_ImmutableArrayAsync_31;

	[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
	[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
	[global::System.Runtime.CompilerServices.AsyncMethodBuilderAttribute(typeof(global::System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder<>))]
	public async partial global::System.Threading.Tasks.ValueTask<global::System.Collections.Immutable.ImmutableArray<global::SomeType>> ImmutableArrayAsync(global::System.Data.Common.DbConnection connection, int id, string name)
	{
		// locals
		global::System.Data.Common.DbCommand? __dapper__command = null;
		global::System.Data.Common.DbDataReader? __dapper__reader = null;
		bool __dapper__close = false;
		int[]? __dapper__tokenBuffer = null;
#pragma warning disable CS0618
		global::Dapper.Internal.Collector<global::SomeType> __dapper__result = default;
#pragma warning restore CS0618
		try
		{
			// prepare connection
			if (connection!.State == global::System.Data.ConnectionState.Closed)
			{
				await connection!.OpenAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false);
				__dapper__close = true;
			}

			// prepare command (excluding parameter values)
			if ((__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_ImmutableArrayAsync_31, null)) is null)
			{
				__dapper__command = __dapper__CreateCommand(connection!);
			}
			else
			{
				__dapper__command.Connection = connection;
			}

			// assign parameter values
#pragma warning disable CS0618
			__dapper__command.Parameters[0].Value = global::Dapper.Internal.InternalUtilities.AsValue(id);
			__dapper__command.Parameters[1].Value = global::Dapper.Internal.InternalUtilities.AsValue(name);
#pragma warning restore CS0618

			// execute reader
			const global::System.Data.CommandBehavior __dapper__behavior = global::System.Data.CommandBehavior.SequentialAccess | global::System.Data.CommandBehavior.SingleResult | global::System.Data.CommandBehavior.SingleRow;
			__dapper__reader = await __dapper__command.ExecuteReaderAsync(__dapper__close ? (__dapper__behavior | global::System.Data.CommandBehavior.CloseConnection) : __dapper__behavior, global::System.Threading.CancellationToken.None).ConfigureAwait(false);
			__dapper__close = false; // performed via CommandBehavior

			// process multiple rows
			if (__dapper__reader.HasRows)
			{
				var __dapper__parser = global::Dapper.TypeReader.TryGetReader<global::SomeType>()!;
				var __dapper__tokens = global::Dapper.TypeReader.RentSegment(ref __dapper__tokenBuffer, __dapper__reader.FieldCount);
				__dapper__parser.IdentifyFieldTokensFromSchema(__dapper__reader, __dapper__tokens);
				while (await __dapper__reader.ReadAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false))
				{
					__dapper__result.Add(await __dapper__parser.ReadAsync(__dapper__reader, __dapper__tokens, global::System.Threading.CancellationToken.None).ConfigureAwait(false));
				}
			}
			// consume additional results (ensures errors from the server are observed)
			while (await __dapper__reader.NextResultAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false)) { }

			// TODO: post-process parameters

			// return rowset
			return __dapper__result.ToImmutableArray();
		}
		finally
		{
			// cleanup
			global::Dapper.TypeReader.Return(ref __dapper__tokenBuffer);
			__dapper__result.Dispose();
			if (__dapper__reader is not null) await __dapper__reader.DisposeAsync().ConfigureAwait(false);
			if (__dapper__command is not null)
			{
				__dapper__command.Connection = default;
				__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_ImmutableArrayAsync_31, __dapper__command);
				if (__dapper__command is not null) await __dapper__command.DisposeAsync().ConfigureAwait(false);
			}
			if (__dapper__close) await (connection?.CloseAsync() ?? global::System.Threading.Tasks.Task.CompletedTask).ConfigureAwait(false);
		}

		// command factory for ImmutableArrayAsync
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
			command.CommandType = global::System.Data.CommandType.StoredProcedure;
			command.CommandText = @"sproc";
			var args = command.Parameters;

			var p = command.CreateParameter();
			p.ParameterName = @"id";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.Int32;
			args.Add(p);

			p = command.CreateParameter();
			p.ParameterName = @"name";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.String;
			p.Size = -1;
			args.Add(p);

			return command;
		}
	}


	// available inactive command for ImmutableListAsync (interlocked)
	private static global::System.Data.Common.DbCommand? s___dapper__command_Samples_Async_Multiple_input_cs_ImmutableListAsync_34;

	[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
	[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
	[global::System.Runtime.CompilerServices.AsyncMethodBuilderAttribute(typeof(global::System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder<>))]
	public async partial global::System.Threading.Tasks.ValueTask<global::System.Collections.Immutable.ImmutableList<global::SomeType>> ImmutableListAsync(global::System.Data.Common.DbConnection connection, int id, string name)
	{
		// locals
		global::System.Data.Common.DbCommand? __dapper__command = null;
		global::System.Data.Common.DbDataReader? __dapper__reader = null;
		bool __dapper__close = false;
		int[]? __dapper__tokenBuffer = null;
#pragma warning disable CS0618
		global::Dapper.Internal.Collector<global::SomeType> __dapper__result = default;
#pragma warning restore CS0618
		try
		{
			// prepare connection
			if (connection!.State == global::System.Data.ConnectionState.Closed)
			{
				await connection!.OpenAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false);
				__dapper__close = true;
			}

			// prepare command (excluding parameter values)
			if ((__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_ImmutableListAsync_34, null)) is null)
			{
				__dapper__command = __dapper__CreateCommand(connection!);
			}
			else
			{
				__dapper__command.Connection = connection;
			}

			// assign parameter values
#pragma warning disable CS0618
			__dapper__command.Parameters[0].Value = global::Dapper.Internal.InternalUtilities.AsValue(id);
			__dapper__command.Parameters[1].Value = global::Dapper.Internal.InternalUtilities.AsValue(name);
#pragma warning restore CS0618

			// execute reader
			const global::System.Data.CommandBehavior __dapper__behavior = global::System.Data.CommandBehavior.SequentialAccess | global::System.Data.CommandBehavior.SingleResult | global::System.Data.CommandBehavior.SingleRow;
			__dapper__reader = await __dapper__command.ExecuteReaderAsync(__dapper__close ? (__dapper__behavior | global::System.Data.CommandBehavior.CloseConnection) : __dapper__behavior, global::System.Threading.CancellationToken.None).ConfigureAwait(false);
			__dapper__close = false; // performed via CommandBehavior

			// process multiple rows
			if (__dapper__reader.HasRows)
			{
				var __dapper__parser = global::Dapper.TypeReader.TryGetReader<global::SomeType>()!;
				var __dapper__tokens = global::Dapper.TypeReader.RentSegment(ref __dapper__tokenBuffer, __dapper__reader.FieldCount);
				__dapper__parser.IdentifyFieldTokensFromSchema(__dapper__reader, __dapper__tokens);
				while (await __dapper__reader.ReadAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false))
				{
					__dapper__result.Add(await __dapper__parser.ReadAsync(__dapper__reader, __dapper__tokens, global::System.Threading.CancellationToken.None).ConfigureAwait(false));
				}
			}
			// consume additional results (ensures errors from the server are observed)
			while (await __dapper__reader.NextResultAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(false)) { }

			// TODO: post-process parameters

			// return rowset
			return __dapper__result.ToImmutableList();
		}
		finally
		{
			// cleanup
			global::Dapper.TypeReader.Return(ref __dapper__tokenBuffer);
			__dapper__result.Dispose();
			if (__dapper__reader is not null) await __dapper__reader.DisposeAsync().ConfigureAwait(false);
			if (__dapper__command is not null)
			{
				__dapper__command.Connection = default;
				__dapper__command = global::System.Threading.Interlocked.Exchange(ref s___dapper__command_Samples_Async_Multiple_input_cs_ImmutableListAsync_34, __dapper__command);
				if (__dapper__command is not null) await __dapper__command.DisposeAsync().ConfigureAwait(false);
			}
			if (__dapper__close) await (connection?.CloseAsync() ?? global::System.Threading.Tasks.Task.CompletedTask).ConfigureAwait(false);
		}

		// command factory for ImmutableListAsync
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
			command.CommandType = global::System.Data.CommandType.StoredProcedure;
			command.CommandText = @"sproc";
			var args = command.Parameters;

			var p = command.CreateParameter();
			p.ParameterName = @"id";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.Int32;
			args.Add(p);

			p = command.CreateParameter();
			p.ParameterName = @"name";
			p.Direction = global::System.Data.ParameterDirection.Input;
			p.DbType = global::System.Data.DbType.String;
			p.Size = -1;
			args.Add(p);

			return command;
		}
	}
}

namespace Dapper.Internal.__dapper__Run_TypeReaders
{
	[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
	file sealed class SomeType : global::Dapper.TypeReader<global::SomeType>
	{
		private SomeType() { }
		public static readonly SomeType Instance = new();

		/// <inheritdoc/>
		public override int GetToken(int token, global::System.Type type, bool isNullable) => token;

		/// <inheritdoc/>
		public override global::SomeType Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset)
		{
			global::SomeType obj = new();
			return obj;
		}
	}
	[global::System.Runtime.CompilerServices.SkipLocalsInitAttribute]
	file sealed class  : global::Dapper.TypeReader<global::SomeType[]>
	{
		private () { }
		public static readonly  Instance = new();

		/// <inheritdoc/>
		public override int GetToken(int token, global::System.Type type, bool isNullable) => token;

		/// <inheritdoc/>
		public override global::SomeType[] Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset)
		{
			global::SomeType[] obj = new();
			return obj;
		}
	}
}
#endregion

# Dapper.AOT.Test - Integration

This is a project for integration tests against a non-mocked database using generated interceptor bits.

### Requirements
Make sure you have Docker Desktop running to be able to initialize a container with PostgreSQL db (or other).

### How to add a new test
1) Add your tests to `Dapper.AOT.Test.Integration` and inherit the [IntegrationTestBase](./Setup/IntegrationTestsBase.cs) class 
2) Override `SetupDatabase` with in example creating a table and filling it in with some data to query later
3) Call `IntegrationTestBase.ExecuteInterceptedUserCode<TUsage, TPoco>()` where:
   - `TUsage` is a type specified in [Dapper.AOT.Test.Integration.Executables/UserCode](../Dapper.AOT.Test.Integration.Executables/UserCode) project
      and implements [IExecutable](../Dapper.AOT.Test.Integration.Executables/IExecutable.cs) interface.
      The method `IExecutable.Execute(DbConnection connection)` will be executed by the framework automatically
   - `TPoco` is the return type of `IExecutable.Execute(DbConnection connection)`. `TPoco` has to be also defined in [Dapper.AOT.Test.Integration.Executables/Models](../Dapper.AOT.Test.Integration.Executables/Models)
4) Assert that returned `TPoco` has all the data you expected

### How test framework works

- Create compilation with reference to user code ([Dapper.AOT.Test.Integration.Executables](../Dapper.AOT.Test.Integration.Executables)) via `CSharpCompilation.Create()`
- give it to `ISourceGenerator` and get an output compilation
- call `outputCompilation.Emit()` and get an assembly
- create instance of user code via reflection (Activator) - this is where we need the `IExecutable` interface to properly cast the type to it
- call `IExecutable.Execute(DbConnection connection)` passing the connection to container-db
- get the output `object`, cast it back to `TPoco` (also defined in [Dapper.AOT.Test.Integration.Executables](../Dapper.AOT.Test.Integration.Executables)) and return it to the test.
  Developer can write assertions based on the returned object

_note_: SourceGenerator is specifically created with the injected `IInterceptorRecorder`, which gets the stack trace of execution and verifies that generated bits were executed.
This gives a full confidence that Interceptors worked as expected. Also it reports diagnostics if something unexpected occured, and test verifies there are none of those. 
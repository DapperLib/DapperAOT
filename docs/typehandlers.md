# Type Handlers

Dapper AOT provides a mechanism to customize how specific .NET types are mapped to and from database values.
This is achieved through Type Handlers, which allow you to define custom serialization for parameters sent 
to the database and custom deserialization for values read from query results. 
This is similar to SqlMapper.TypeHandler in vanilla Dapper, but with a specific AOT-compatible registration process.

To register your custom type handler, you use either an assembly or module level attribute. 
Both have the same effect.
This attribute tells Dapper AOT which .NET type (TValue) your handler processes 
and which class (TTypeHandler) is responsible for that processing.

``` csharp
// Example using a module-level attribute
using Dapper;

[module: TypeHandler<YourDotNetType, YourCustomTypeHandler>]

// Example using an assembly-level attribute (often in AssemblyInfo.cs or a shared file)
// [assembly: TypeHandler<YourDotNetType, YourCustomTypeHandler>]
```

Your custom type handler class must:
* Inherit from Dapper.TypeHandler<TValue>, where TValue is the .NET type it handles.
* Have a public parameterless constructor (new()).
* Implement (override) the necessary virtual methods for your specific conversion needs.
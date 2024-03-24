# Type Handlers

At times you might want to customise how a type is read from a query or how it
is saved in a parameter. In Dapper you might use a `SqlMapper.TypeHandler` for
this, which has a slightly altered interface in the AOT version and a different
way of registering them.

To register your own type handler, use either an assembly or module level
attribute to specify the mapping (you can replace `module` with `assembly`
below, it has the same effect):

``` csharp
using Dapper;

[module: TypeHandler<MyClass, MyClassTypeHandler>]
```

Your type handler must inherit from `Dapper.TypeHandler<T>` and be default
constructable. The methods are virtual, so you can override only which ones you
need (e.g. if you're just interested in reading your values and not using them
as parameters, you only need to override the `Read` method).
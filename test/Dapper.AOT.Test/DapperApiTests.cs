using Dapper.CodeAnalysis;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;

namespace Dapper.AOT.Test;

public class DapperApiTests
{
    private readonly ITestOutputHelper Log;
    public DapperApiTests(ITestOutputHelper log) => Log = log;
    [Fact]
    public void DiscoveredMethodsAreExpected()
    {
        var methods = (from method in typeof(SqlMapper).GetMethods()
                       where method.IsPublic && method.IsStatic && method.IsDefined(typeof(ExtensionAttribute), false)
                       select method.IsGenericMethod ? (method.Name + "<") : method.Name).Distinct().ToArray();
        Array.Sort(methods);
        var names = string.Join(',', methods);
        Log.WriteLine(names);

        Assert.Equal("AsList<,AsTableValuedParameter,AsTableValuedParameter<,Execute,ExecuteAsync,ExecuteReader,ExecuteReaderAsync,ExecuteScalar,ExecuteScalar<,ExecuteScalarAsync,ExecuteScalarAsync<,GetRowParser,GetRowParser<,GetTypeName,Parse,Parse<,Query,Query<,QueryAsync,QueryAsync<,QueryFirst,QueryFirst<,QueryFirstAsync,QueryFirstAsync<,QueryFirstOrDefault,QueryFirstOrDefault<,QueryFirstOrDefaultAsync,QueryFirstOrDefaultAsync<,QueryMultiple,QueryMultipleAsync,QuerySingle,QuerySingle<,QuerySingleAsync,QuerySingleAsync<,QuerySingleOrDefault,QuerySingleOrDefault<,QuerySingleOrDefaultAsync,QuerySingleOrDefaultAsync<,QueryUnbufferedAsync,QueryUnbufferedAsync<,ReplaceLiterals,SetTypeName", names);

        var candidates = string.Join(',', methods.Where(DapperInterceptorGenerator.IsCandidate));
        Log.WriteLine(candidates);
        Assert.Equal("Execute,ExecuteAsync,ExecuteReader,ExecuteReaderAsync,ExecuteScalar,ExecuteScalar<,ExecuteScalarAsync,ExecuteScalarAsync<,Query,Query<,QueryAsync,QueryAsync<,QueryFirst,QueryFirst<,QueryFirstAsync,QueryFirstAsync<,QueryFirstOrDefault,QueryFirstOrDefault<,QueryFirstOrDefaultAsync,QueryFirstOrDefaultAsync<,QueryMultiple,QueryMultipleAsync,QuerySingle,QuerySingle<,QuerySingleAsync,QuerySingleAsync<,QuerySingleOrDefault,QuerySingleOrDefault<,QuerySingleOrDefaultAsync,QuerySingleOrDefaultAsync<,QueryUnbufferedAsync,QueryUnbufferedAsync<", candidates);
    }
}

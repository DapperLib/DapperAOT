using System.Data;

namespace Dapper.AOT.Test.Integration.Executables;

public interface IExecutable<T>
{
    public T Execute(IDbConnection connection);
}
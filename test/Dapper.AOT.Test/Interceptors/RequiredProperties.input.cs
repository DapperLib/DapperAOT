using Dapper;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

[DapperAot]

public class SomeType
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public bool IsComplete { get; set; }

    public Task<SomeType> Insert(SqlConnection connection)
        => connection.QuerySingleAsync<SomeType>("""
            INSERT INTO MyTable(Title, IsComplete)
            Values(@Title, @IsComplete)
            RETURNING *
        """, this);
}


namespace System.Runtime.CompilerServices
{
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    sealed file class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string _) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Field | System.AttributeTargets.Property | System.AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    sealed file class RequiredMemberAttribute : Attribute {}
}
namespace System.Diagnostics.CodeAnalysis
{
    [System.AttributeUsage(System.AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    sealed file class SetsRequiredMembersAttribute : Attribute { }
}
using System;
using System.Data.Common;

namespace Dapper
{
	/// <summary>
	/// Captures the records-affected when available on a query; this is the return value of <see cref="DbCommand.ExecuteNonQuery"/>, or
	/// <see cref="DbDataReader.RecordsAffected"/> via <see cref="DbCommand.ExecuteReader()"/>; it has no defined meaning for <see cref="DbCommand.ExecuteScalar"/>.
	/// Note that this value is not reliable, and requires testing in your specific scenario and configuration; for example, it is impacted by <c>SET NOCOUNT ON</c>.
	/// </summary>
	[AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public sealed class RecordsAffectedAttribute : Attribute { }
}

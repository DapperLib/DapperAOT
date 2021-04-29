using System;
using System.Data;
using System.Data.Common;

namespace Dapper
{
	/// <summary>
	/// Allows fine-grained control over how parameters are defined
	/// </summary>
	[AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public sealed class ParameterAttribute : Attribute
	{
		/// <summary>
		/// See <see cref="DbParameter.Direction"/>
		/// </summary>
		public ParameterDirection Direction { get; set; }
		/// <summary>
		/// See <see cref="DbParameter.ParameterName"/>
		/// </summary>
		public string Name { get; set; } = "";
		/// <summary>
		/// See <see cref="DbParameter.DbType"/>
		/// </summary>
		public DbType DbType { get; set; }
		/// <summary>
		/// See <see cref="DbParameter.Size"/>
		/// </summary>
		public int Size { get; set; }
		/// <summary>
		/// See <see cref="DbParameter.Precision"/>
		/// </summary>
		public byte Precision { get; set; }
		/// <summary>
		/// See <see cref="DbParameter.Scale"/>
		/// </summary>
		public byte Scale { get; set; }
	}
}

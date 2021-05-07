using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Dapper
{
	/// <summary>
	/// Works identitally to <see href="https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommandcolumnencryptionsetting"><c>SqlCommandColumnEncryptionSetting</c></see>
	/// </summary>
	public enum EncryptionKind
	{
		/// <summary>
		/// Specifies that the command should default to the Always Encrypted setting in the connection string.
		/// </summary>
		UseConnectionSetting = 0,
		/// <summary>
		/// Disables Always Encrypted for the query.
		/// </summary>
		Disabled = 3,
		/// <summary>
		/// Enables Always Encrypted for the query.
		/// </summary>
		Enabled = 1,
		/// <summary>
		/// Specifies that only the results of the command should be processed by the Always Encrypted routine in the driver. Use this value when the command has no parameters that require encryption.
		/// </summary>
		ResultSetOnly = 2,
	}

	/// <summary>
	/// Specifies the encryption options to apply to this command.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	[Conditional("DEBUG"), ImmutableObject(true)]
	public sealed class EncryptionAttribute : Attribute
	{
		/// <summary>
		/// Specifies the encryption options to apply to this command.
		/// </summary>
		public EncryptionKind EncryptionKind { get; }
		/// <summary>
		/// Creates a new <see cref="EncryptionAttribute"/> instance.
		/// </summary>
		/// <param name="encryptionKind">Specifies the encryption options to apply to this command.</param>
		public EncryptionAttribute(EncryptionKind encryptionKind)
			=> EncryptionKind = encryptionKind;
	}
}

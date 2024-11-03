using System;
using System.ComponentModel;

namespace Dapper;

/// <summary>
/// Indicates that an enum should be processed as a string
/// </summary>
[ImmutableObject(true)]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = false)]
public sealed class EnumStringAttribute : Attribute { }
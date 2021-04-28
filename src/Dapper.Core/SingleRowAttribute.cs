using System;
using System.ComponentModel;

namespace Dapper
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [ImmutableObject(true)]
    public sealed class SingleRowAttribute : Attribute
    {
        public SingleRowKind Kind { get; }
        public SingleRowAttribute(SingleRowKind kind = SingleRowKind.Automatic)
            => Kind = kind;
    }
    public enum SingleRowKind
    {
        Automatic = -1,
        // note: the following *could* also be flags, "1=demand at least one row", "2=demand at most one row"
        FirstOrDefault = 0,
        First = 1,
        SingleOrDefault = 2,
        Single = 3,
        
    }
}

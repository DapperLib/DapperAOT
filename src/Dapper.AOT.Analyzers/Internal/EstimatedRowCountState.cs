using System;

namespace Dapper.Internal;

internal readonly struct EstimatedRowCountState : IEquatable<EstimatedRowCountState>
{
    public readonly int Count;
    public readonly string? MemberName;
    public bool HasValue => Count > 0 || MemberName is not null;

    public EstimatedRowCountState(string memberName)
    {
        Count = 0;
        MemberName = memberName;
    }

    public EstimatedRowCountState(int count)
    {
        Count = count;
        MemberName = null;
    }

    public override bool Equals(object obj) => obj is EstimatedRowCountState other && Equals(other);

    public bool Equals(EstimatedRowCountState other)
        => Count == other.Count && MemberName == other.MemberName;

    public override int GetHashCode()
        => Count + (MemberName is null ? 0 : MemberName.GetHashCode());

    public override string ToString() => MemberName is null
        ? Count.ToString() : MemberName;
}

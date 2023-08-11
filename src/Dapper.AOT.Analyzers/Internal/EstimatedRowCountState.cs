using System;

namespace Dapper.Internal;

internal readonly struct EstimatedRowCountState : IEquatable<EstimatedRowCountState>
{
    public readonly int Count, ResultIndex;
    public readonly string? MemberName;

    public EstimatedRowCountState(string memberName, int resultIndex)
    {
        Count = 0;
        ResultIndex = resultIndex;
        MemberName = memberName;
    }

    public EstimatedRowCountState(int count, int resultIndex)
    {
        Count = count;
        ResultIndex = resultIndex;
        MemberName = null;
    }

    public override bool Equals(object obj) => obj is EstimatedRowCountState other && Equals(other);

    public bool Equals(EstimatedRowCountState other)
        => Count == other.Count && ResultIndex == other.ResultIndex && MemberName == other.MemberName;

    public override int GetHashCode()
        => (Count ^ ResultIndex) + (MemberName is null ? 0 : MemberName.GetHashCode());

    public override string ToString() => MemberName is null
        ? $"grid {ResultIndex}: {Count} rows" : $"grid {ResultIndex}: .{MemberName} rows";
}

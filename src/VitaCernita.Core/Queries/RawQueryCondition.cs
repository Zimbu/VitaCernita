using System;

namespace VitaCernita.Core.Queries;

/// <summary>
/// Represents a raw query condition, preserving an existing Gmail search query string
/// (e.g. as returned from the Gmail API users.settings.filters resource).
/// </summary>
public sealed class RawQueryCondition : IQueryCondition, IEquatable<RawQueryCondition>
{
    public string Query { get; }

    public RawQueryCondition(string query)
    {
        Query = query ?? string.Empty;
    }

    public string ToGmailQuery(bool explicitAnd = false) => Query;

    public override string ToString() => Query;

    public bool Equals(RawQueryCondition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Query, other.Query, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => obj is RawQueryCondition other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Query);
}

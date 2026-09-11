using System;

namespace VitaCernita.Core.Queries;

/// <summary>
/// Represents a Gmail search query criteria condition.
/// </summary>
public class GmailQuery : IQueryCondition, IEquatable<GmailQuery>
{
    public IQueryCondition? Condition { get; set; }

    public GmailQuery(IQueryCondition? condition = null)
    {
        Condition = condition;
    }

    public string ToGmailQuery(bool explicitAnd = false) => Condition?.ToGmailQuery(explicitAnd) ?? string.Empty;

    public override string ToString() => ToGmailQuery();

    public bool Equals(GmailQuery? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(ToGmailQuery(), other.ToGmailQuery(), StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => obj is GmailQuery other && Equals(other);

    public override int GetHashCode() => ToGmailQuery().GetHashCode();
}

using System;
using VitaCernita.Core.Filters;

namespace VitaCernita.Core.Queries;

/// <summary>
/// Represents a negation (NOT) query condition, emitting the '-' prefix in Gmail search queries.
/// </summary>
public sealed class NotCondition : IQueryCondition, IFilterCondition, IEquatable<NotCondition>
{
    public IQueryCondition InnerCondition { get; }

    public NotCondition(IQueryCondition innerCondition)
    {
        InnerCondition = innerCondition ?? throw new ArgumentNullException(nameof(innerCondition));
    }

    public string ToGmailQuery(bool explicitAnd = false)
    {
        string innerQuery = InnerCondition.ToGmailQuery(explicitAnd);
        if (string.IsNullOrWhiteSpace(innerQuery))
        {
            return string.Empty;
        }

        if (InnerCondition is AndCondition andCond && andCond.Conditions.Count > 1)
        {
            return $"-({innerQuery})";
        }

        if (InnerCondition is OrCondition orCond && orCond.Conditions.Count > 1)
        {
            return $"-({innerQuery})";
        }

        if (InnerCondition is NotCondition)
        {
            return $"-({innerQuery})";
        }

        return $"-{innerQuery}";
    }

    public bool Equals(NotCondition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return InnerCondition.Equals(other.InnerCondition);
    }

    public override bool Equals(object? obj)
    {
        return obj is NotCondition other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(InnerCondition);
    }

    public override string ToString() => ToGmailQuery();
}

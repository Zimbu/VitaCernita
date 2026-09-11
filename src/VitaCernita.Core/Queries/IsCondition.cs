using System;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Queries.Validation;

namespace VitaCernita.Core.Queries;

/// <summary>
/// Represents a Gmail 'is:' operator condition (e.g. is:starred).
/// </summary>
public sealed class IsCondition : IQueryCondition, IFilterCondition, IEquatable<IsCondition>
{
    public string Target { get; }

    public IsCondition(string target)
    {
        Target = QueryValidator.ValidateAndNormalizeIsTarget("is", target);
    }

    public string ToGmailQuery(bool explicitAnd = false)
    {
        return $"is:{Target}";
    }

    public bool Equals(IsCondition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Target, other.Target, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is IsCondition other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Target);
    }

    public override string ToString() => ToGmailQuery();
}

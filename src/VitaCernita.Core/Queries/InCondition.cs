using System;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Queries.Validation;

namespace VitaCernita.Core.Queries;

/// <summary>
/// Represents a Gmail 'in:' operator condition for locations and folders
/// (e.g. in:anywhere, in:archive, in:snoozed, in:inbox, in:trash, in:spam).
/// </summary>
public sealed class InCondition : IQueryCondition, IFilterCondition, IEquatable<InCondition>
{
    public string Target { get; }

    public InCondition(string target)
    {
        Target = QueryValidator.ValidateAndNormalizeInTarget("in", target);
    }

    public string ToGmailQuery(bool explicitAnd = false)
    {
        return $"in:{Target}";
    }

    public bool Equals(InCondition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Target, other.Target, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is InCondition other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Target);
    }

    public override string ToString() => ToGmailQuery();
}

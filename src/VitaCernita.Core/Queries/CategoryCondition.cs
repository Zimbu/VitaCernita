using System;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Queries.Validation;

namespace VitaCernita.Core.Queries;

/// <summary>
/// Represents a Gmail 'category:' operator condition
/// (e.g. category:primary, category:social, category:promotions, category:updates, category:forums, category:reservations, category:purchases).
/// </summary>
public sealed class CategoryCondition : IQueryCondition, IFilterCondition, IEquatable<CategoryCondition>
{
    public string Target { get; }

    public CategoryCondition(string target)
    {
        Target = QueryValidator.ValidateAndNormalizeCategoryTarget("category", target);
    }

    public string ToGmailQuery(bool explicitAnd = false)
    {
        return $"category:{Target}";
    }

    public bool Equals(CategoryCondition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Target, other.Target, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is CategoryCondition other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Target);
    }

    public override string ToString() => ToGmailQuery();
}

using System;
using VitaCernita.Core.Filters.Validation;

namespace VitaCernita.Core.Filters;

/// <summary>
/// Represents a Gmail 'has:' operator condition for stars and icons
/// (e.g. has:yellow-star, has:red-bang, has:orange-guillemet).
/// </summary>
public sealed class HasCondition : IFilterCondition, IEquatable<HasCondition>
{
    public string Target { get; }

    public HasCondition(string target)
    {
        Target = FilterValidator.ValidateAndNormalizeStar("has", target);
    }

    public string ToGmailQuery(bool explicitAnd = false)
    {
        return $"has:{Target}";
    }

    public bool Equals(HasCondition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Target, other.Target, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is HasCondition other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Target);
    }

    public override string ToString() => ToGmailQuery();
}

using System;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Queries.Validation;

namespace VitaCernita.Core.Queries;

/// <summary>
/// Represents a Gmail size comparison operator ('size:', 'larger:', 'smaller:').
/// </summary>
public sealed class SizeCondition : IQueryCondition, IFilterCondition, IEquatable<SizeCondition>
{
    public string Operator { get; }
    public string Size { get; }

    public SizeCondition(string op, string size)
    {
        Operator = QueryValidator.ValidateAndNormalizeSizeOperator(op);
        Size = QueryValidator.ValidateAndNormalizeSize(Operator, size);
    }

    public string ToGmailQuery(bool explicitAnd = false)
    {
        return $"{Operator}:{Size}";
    }

    public bool Equals(SizeCondition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Operator, other.Operator, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Size, other.Size, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is SizeCondition other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(Operator),
            StringComparer.OrdinalIgnoreCase.GetHashCode(Size));
    }

    public override string ToString() => ToGmailQuery();
}

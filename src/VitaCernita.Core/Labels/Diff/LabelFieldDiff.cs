using System;

namespace VitaCernita.Core.Labels.Diff;

/// <summary>
/// Represents a difference in a single configurable field of a label.
/// </summary>
public sealed class LabelFieldDiff : IEquatable<LabelFieldDiff>
{
    public string FieldName { get; }
    public object? CurrentValue { get; }
    public object? DesiredValue { get; }
    public bool IsDifferent => !Equals(CurrentValue, DesiredValue);

    public LabelFieldDiff(string fieldName, object? currentValue, object? desiredValue)
    {
        FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        CurrentValue = currentValue;
        DesiredValue = desiredValue;
    }

    private static string FormatValue(object? val)
    {
        if (val == null) return "<unset>";
        if (val is LabelColor lc) return $"[{lc.TextColor} / {lc.BackgroundColor}]";
        return val.ToString() ?? "<unset>";
    }

    public override string ToString() => $"{FieldName}: {FormatValue(CurrentValue)} -> {FormatValue(DesiredValue)}";

    public bool Equals(LabelFieldDiff? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(FieldName, other.FieldName, StringComparison.OrdinalIgnoreCase) &&
               Equals(CurrentValue, other.CurrentValue) &&
               Equals(DesiredValue, other.DesiredValue);
    }

    public override bool Equals(object? obj) => obj is LabelFieldDiff other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(FieldName.ToLowerInvariant(), CurrentValue, DesiredValue);
}

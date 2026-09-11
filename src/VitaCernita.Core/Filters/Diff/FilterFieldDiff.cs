using System;

namespace VitaCernita.Core.Filters.Diff;

/// <summary>
/// Represents a difference in a single configurable field of a Gmail filter.
/// </summary>
public sealed class FilterFieldDiff
{
    public string FieldName { get; }
    public object? CurrentValue { get; }
    public object? DesiredValue { get; }

    public FilterFieldDiff(string fieldName, object? currentValue, object? desiredValue)
    {
        FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        CurrentValue = currentValue;
        DesiredValue = desiredValue;
    }

    public override string ToString()
    {
        string curStr = CurrentValue?.ToString() ?? "<unset>";
        string desStr = DesiredValue?.ToString() ?? "<unset>";
        return $"{FieldName}: '{curStr}' -> '{desStr}'";
    }
}

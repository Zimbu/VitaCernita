using System;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Diffing;

/// <summary>
/// Represents a value difference on a single property or field between current and desired state.
/// </summary>
/// <param name="FieldName">The property or field name that differs.</param>
/// <param name="CurrentValue">The value currently on the server/source (null if not set).</param>
/// <param name="DesiredValue">The desired value from the local configuration (null if unset/removed).</param>
public sealed record FieldDiff(string FieldName, object? CurrentValue, object? DesiredValue)
{
    private static string FormatValue(object? val)
    {
        if (val == null) return "<unset>";
        if (val is LabelColor lc) return $"[{lc.TextColor} / {lc.BackgroundColor}]";
        return val.ToString() ?? "<unset>";
    }

    public override string ToString() =>
        $"{FieldName}: {FormatValue(CurrentValue)} -> {FormatValue(DesiredValue)}";
}

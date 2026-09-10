using System;

namespace VitaCernita.Core.Filters;

/// <summary>
/// Condition matching a specific email field (e.g. 'from', 'subject').
/// </summary>
public sealed class FieldCondition : IFilterCondition
{
    public string Field { get; }
    public string Value { get; }

    public FieldCondition(string field, string value)
    {
        Field = field?.Trim().ToLowerInvariant() ?? throw new ArgumentNullException(nameof(field));
        Value = value?.Trim() ?? string.Empty;
    }

    public string ToGmailQuery(bool explicitAnd = false)
    {
        string formattedValue = FormatValue(Value);
        return $"{Field}:{formattedValue}";
    }

    private static string FormatValue(string val)
    {
        if (string.IsNullOrEmpty(val))
        {
            return "\"\"";
        }

        // If wrapped in matching quotes already, strip them
        if (val.Length >= 2 && val.StartsWith('"') && val.EndsWith('"'))
        {
            val = val[1..^1];
        }

        // Quote if value contains whitespace, quotes, or punctuation
        if (val.Contains(' ') || val.Contains('\t') || val.Contains('"'))
        {
            string escaped = val.Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }

        return val;
    }

    public override string ToString() => ToGmailQuery();
}

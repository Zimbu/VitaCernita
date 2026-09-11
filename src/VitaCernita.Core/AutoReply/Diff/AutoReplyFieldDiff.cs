namespace VitaCernita.Core.AutoReply.Diff;

/// <summary>
/// Represents a difference in a single configurable field of an AutoReply (VacationSettings) setting.
/// </summary>
public sealed class AutoReplyFieldDiff
{
    public string FieldName { get; }
    public object? CurrentValue { get; }
    public object? DesiredValue { get; }

    public AutoReplyFieldDiff(string fieldName, object? currentValue, object? desiredValue)
    {
        FieldName = fieldName;
        CurrentValue = currentValue;
        DesiredValue = desiredValue;
    }

    public override string ToString()
    {
        string curStr = CurrentValue != null ? FormatValue(CurrentValue) : "<unset>";
        string desStr = DesiredValue != null ? FormatValue(DesiredValue) : "<unset>";
        return $"{FieldName}: {curStr} -> {desStr}";
    }

    private static string FormatValue(object val)
    {
        if (val is string s)
        {
            if (s.Length > 30) return $"'{s[..27]}...'";
            return $"'{s.Replace("\n", " ")}'";
        }
        return val.ToString() ?? string.Empty;
    }
}

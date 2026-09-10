using System;

namespace VitaCernita.Core.Filters;

/// <summary>
/// Relative duration condition (older_than, newer_than). Formatted as operator:Nd/Nm/Ny per Gmail spec.
/// </summary>
public sealed class DurationCondition : IFilterCondition
{
    public string Operator { get; }
    public string Duration { get; }

    public DurationCondition(string op, string duration)
    {
        Operator = op?.Trim().ToLowerInvariant() ?? throw new ArgumentNullException(nameof(op));
        Duration = duration?.Trim().ToLowerInvariant() ?? throw new ArgumentNullException(nameof(duration));
    }

    public string ToGmailQuery(bool explicitAnd = false)
    {
        return $"{Operator}:{Duration}";
    }

    public override string ToString() => ToGmailQuery();
}

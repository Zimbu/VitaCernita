using System;
using System.Globalization;
using VitaCernita.Core.Filters;

namespace VitaCernita.Core.Queries;

/// <summary>
/// Date filter condition (after, before, older, newer). Formatted as yyyy/MM/dd per Gmail spec.
/// </summary>
public sealed class DateCondition : IQueryCondition, IFilterCondition
{
    public string Operator { get; }
    public DateTime Date { get; }

    public DateCondition(string op, DateTime date)
    {
        Operator = op?.Trim().ToLowerInvariant() ?? throw new ArgumentNullException(nameof(op));
        Date = date;
    }

    public string ToGmailQuery(bool explicitAnd = false)
    {
        return $"{Operator}:{Date.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)}";
    }

    public override string ToString() => ToGmailQuery();
}

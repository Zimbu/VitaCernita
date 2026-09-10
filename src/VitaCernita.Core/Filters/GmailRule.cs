using System;

namespace VitaCernita.Core.Filters;

/// <summary>
/// A Gmail filter rule consisting of a matching condition and metadata.
/// </summary>
public sealed class GmailRule
{
    public string? Name { get; set; }
    public IFilterCondition Condition { get; }

    public GmailRule(IFilterCondition condition, string? name = null)
    {
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        Name = name;
    }

    public string ToGmailQuery(bool explicitAnd = false) => Condition.ToGmailQuery(explicitAnd);

    public override string ToString() => ToGmailQuery();
}

using System;
using VitaCernita.Core.Actions;

namespace VitaCernita.Core.Filters;

/// <summary>
/// Represents a Gmail filter consisting of search criteria (query) and actions to perform.
/// The query criteria and actions are decoupled and can be used together or separately.
/// </summary>
public class GmailFilter
{
    public string? Name { get; set; }
    public IFilterCondition? Criteria { get; set; }
    public GmailAction? Action { get; set; }

    public GmailFilter(IFilterCondition? criteria = null, GmailAction? action = null, string? name = null)
    {
        Criteria = criteria;
        Action = action;
        Name = name;
    }

    public string ToGmailQuery(bool explicitAnd = false) => Criteria?.ToGmailQuery(explicitAnd) ?? string.Empty;

    public override string ToString() => ToGmailQuery();
}

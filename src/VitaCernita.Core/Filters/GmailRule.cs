using System;
using VitaCernita.Core.Actions;

namespace VitaCernita.Core.Filters;

/// <summary>
/// A Gmail filter rule consisting of a matching condition, action, and metadata.
/// Inherits from GmailFilter to maintain full backward compatibility while supporting decoupled actions.
/// </summary>
public sealed class GmailRule : GmailFilter
{
    public IFilterCondition? Condition => Criteria;

    public GmailRule(IFilterCondition? condition = null, string? name = null, GmailAction? action = null)
        : base(condition, action, name)
    {
    }
}

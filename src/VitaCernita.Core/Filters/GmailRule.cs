using System;
using VitaCernita.Core.Actions;
using VitaCernita.Core.Queries;

namespace VitaCernita.Core.Filters;

/// <summary>
/// A Gmail filter rule consisting of an optional id, matching query condition, action, and metadata.
/// Inherits from <see cref="GmailFilter"/> to maintain full backward compatibility.
/// </summary>
public sealed class GmailRule : GmailFilter
{
    public GmailRule(
        IQueryCondition? condition = null,
        string? name = null,
        GmailAction? action = null,
        string? id = null)
        : base(id: id, query: condition, action: action, name: name)
    {
    }
}

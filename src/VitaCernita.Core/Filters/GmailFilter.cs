using System;
using System.Collections.Generic;
using VitaCernita.Core.Actions;
using VitaCernita.Core.Queries;

namespace VitaCernita.Core.Filters;

/// <summary>
/// Represents a Gmail filter consisting of an id, search query criteria, and actions to perform.
/// Maps directly to the Google Gmail API users.settings.filters resource:
/// { id = "...", criteria = { query = "..." }, action = { ... } }
/// </summary>
public class GmailFilter : IEquatable<GmailFilter>
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public IQueryCondition? Query { get; set; }
    public GmailAction? Action { get; set; }

    /// <summary>
    /// Backward compatibility alias for <see cref="Query"/>.
    /// </summary>
    public IQueryCondition? Criteria
    {
        get => Query;
        set => Query = value;
    }

    /// <summary>
    /// Backward compatibility alias for <see cref="Query"/>.
    /// </summary>
    public IQueryCondition? Condition
    {
        get => Query;
        set => Query = value;
    }

    public GmailFilter(
        string? id = null,
        IQueryCondition? query = null,
        GmailAction? action = null,
        string? name = null)
    {
        Id = id;
        Query = query;
        Action = action;
        Name = name;
    }

    public GmailFilter(IQueryCondition? criteria, GmailAction? action = null, string? name = null)
        : this(id: null, query: criteria, action: action, name: name)
    {
    }

    public string ToGmailQuery(bool explicitAnd = false) => Query?.ToGmailQuery(explicitAnd) ?? string.Empty;

    /// <summary>
    /// Formats this filter into a dictionary matching the Gmail API filter resource specification.
    /// </summary>
    public Dictionary<string, object> ToDictionary(bool explicitAnd = false)
    {
        var dict = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(Id))
        {
            dict["id"] = Id;
        }

        string queryString = ToGmailQuery(explicitAnd);
        if (!string.IsNullOrEmpty(queryString))
        {
            dict["criteria"] = new Dictionary<string, object>
            {
                ["query"] = queryString
            };
        }

        if (Action != null && !Action.IsEmpty)
        {
            dict["action"] = Action.ToDictionary();
        }

        return dict;
    }

    public override string ToString() => ToGmailQuery();

    public bool Equals(GmailFilter? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Id, other.Id, StringComparison.Ordinal) &&
               string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               string.Equals(ToGmailQuery(), other.ToGmailQuery(), StringComparison.Ordinal) &&
               Equals(Action, other.Action);
    }

    public override bool Equals(object? obj) => obj is GmailFilter other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, Name, ToGmailQuery(), Action);
}

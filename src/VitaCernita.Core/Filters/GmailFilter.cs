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

    /// <summary>
    /// Deserializes a GmailFilter from a JSON string.
    /// </summary>
    public static GmailFilter FromJson(string json)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return FromJsonElement(doc.RootElement);
    }

    /// <summary>
    /// Deserializes a GmailFilter from a JsonElement matching the Gmail API users.settings.filters resource.
    /// </summary>
    public static GmailFilter FromJsonElement(System.Text.Json.JsonElement element)
    {
        string? id = element.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        IQueryCondition? query = null;
        GmailAction? action = null;

        if (element.TryGetProperty("criteria", out var critProp) && critProp.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            var parts = new List<string>();
            if (critProp.TryGetProperty("query", out var qProp) && !string.IsNullOrWhiteSpace(qProp.GetString()))
            {
                parts.Add(qProp.GetString()!);
            }
            if (critProp.TryGetProperty("from", out var fromProp) && !string.IsNullOrWhiteSpace(fromProp.GetString()))
            {
                parts.Add($"from:{fromProp.GetString()}");
            }
            if (critProp.TryGetProperty("to", out var toProp) && !string.IsNullOrWhiteSpace(toProp.GetString()))
            {
                parts.Add($"to:{toProp.GetString()}");
            }
            if (critProp.TryGetProperty("subject", out var subProp) && !string.IsNullOrWhiteSpace(subProp.GetString()))
            {
                parts.Add($"subject:{subProp.GetString()}");
            }
            if (critProp.TryGetProperty("negatedQuery", out var negProp) && !string.IsNullOrWhiteSpace(negProp.GetString()))
            {
                parts.Add($"-({negProp.GetString()})");
            }
            if (critProp.TryGetProperty("hasAttachment", out var attProp) && attProp.ValueKind == System.Text.Json.JsonValueKind.True)
            {
                parts.Add("has:attachment");
            }
            if (critProp.TryGetProperty("size", out var szProp) && szProp.TryGetInt64(out var sz))
            {
                string comparison = critProp.TryGetProperty("sizeComparison", out var scProp) ? (scProp.GetString() ?? "larger") : "larger";
                parts.Add($"{comparison}:{sz}");
            }

            if (parts.Count > 0)
            {
                query = new RawQueryCondition(string.Join(" ", parts));
            }
        }

        if (element.TryGetProperty("action", out var actProp) && actProp.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            action = GmailAction.FromJsonElement(actProp);
        }

        return new GmailFilter(id, query, action);
    }

    /// <summary>
    /// Deserializes a GmailFilter from a dictionary.
    /// </summary>
    public static GmailFilter FromDictionary(IReadOnlyDictionary<string, object?> dict)
    {
        string? id = dict.TryGetValue("id", out var idVal) ? idVal?.ToString() : null;
        IQueryCondition? query = null;
        GmailAction? action = null;

        if (dict.TryGetValue("criteria", out var critVal) && critVal != null)
        {
            if (critVal is System.Text.Json.JsonElement je)
            {
                var f = FromJsonElement(je);
                query = f.Query;
            }
            else if (critVal is IReadOnlyDictionary<string, object?> cd)
            {
                var parts = new List<string>();
                if (cd.TryGetValue("query", out var q) && q != null && !string.IsNullOrWhiteSpace(q.ToString()))
                {
                    parts.Add(q.ToString()!);
                }
                if (cd.TryGetValue("from", out var from) && from != null && !string.IsNullOrWhiteSpace(from.ToString()))
                {
                    parts.Add($"from:{from}");
                }
                if (cd.TryGetValue("to", out var to) && to != null && !string.IsNullOrWhiteSpace(to.ToString()))
                {
                    parts.Add($"to:{to}");
                }
                if (cd.TryGetValue("subject", out var sub) && sub != null && !string.IsNullOrWhiteSpace(sub.ToString()))
                {
                    parts.Add($"subject:{sub}");
                }
                if (parts.Count > 0)
                {
                    query = new RawQueryCondition(string.Join(" ", parts));
                }
            }
        }

        if (dict.TryGetValue("action", out var actVal) && actVal != null)
        {
            if (actVal is GmailAction ga)
            {
                action = ga;
            }
            else if (actVal is IReadOnlyDictionary<string, object?> ad)
            {
                action = GmailAction.FromDictionary(ad);
            }
            else if (actVal is System.Text.Json.JsonElement je)
            {
                action = GmailAction.FromJsonElement(je);
            }
        }

        return new GmailFilter(id, query, action);
    }

    /// <summary>
    /// Parses the JSON response from Gmail API users.settings.filters.list into a list of GmailFilter instances.
    /// </summary>
    public static List<GmailFilter> FromApiListResponse(string json)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var filters = new List<GmailFilter>();

        System.Text.Json.JsonElement arrayElement;
        if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            arrayElement = doc.RootElement;
        }
        else if (doc.RootElement.TryGetProperty("filter", out var fProp) && fProp.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            arrayElement = fProp;
        }
        else
        {
            return filters;
        }

        foreach (var item in arrayElement.EnumerateArray())
        {
            filters.Add(FromJsonElement(item));
        }

        return filters;
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

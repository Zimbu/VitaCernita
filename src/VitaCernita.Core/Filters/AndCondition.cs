using System;
using System.Collections.Generic;
using System.Linq;

namespace VitaCernita.Core.Filters;

/// <summary>
/// Conjunction (AND) of multiple filter conditions.
/// </summary>
public sealed class AndCondition : IFilterCondition
{
    private static readonly Dictionary<string, int> FieldOrdering = new(StringComparer.OrdinalIgnoreCase)
    {
        { "from", 1 },
        { "to", 2 },
        { "cc", 3 },
        { "bcc", 4 },
        { "deliveredto", 5 },
        { "subject", 6 },
        { "list", 7 },
        { "filename", 8 },
        { "header", 9 },
        { "rfc822msgid", 10 },
        { "label", 11 },
        { "after", 12 },
        { "before", 13 },
        { "older", 14 },
        { "newer", 15 },
        { "older_than", 16 },
        { "newer_than", 17 }
    };

    public IReadOnlyList<IFilterCondition> Conditions { get; }

    public AndCondition(IEnumerable<IFilterCondition> conditions)
    {
        if (conditions == null) throw new ArgumentNullException(nameof(conditions));

        // Flatten any nested AndConditions for clean canonical representation
        var list = new List<IFilterCondition>();
        foreach (var cond in conditions)
        {
            if (cond is AndCondition nestedAnd)
            {
                list.AddRange(nestedAnd.Conditions);
            }
            else
            {
                list.Add(cond);
            }
        }

        // Canonical ordering to ensure identical queries regardless of declaration order
        Conditions = list.OrderBy(GetSortKey).ToList();
    }

    private static (int priority, string field, string val) GetSortKey(IFilterCondition cond)
    {
        if (cond is FieldCondition fc)
        {
            int priority = FieldOrdering.TryGetValue(fc.Field, out int p) ? p : 20;
            return (priority, fc.Field, fc.Value);
        }
        if (cond is DateCondition dc)
        {
            int priority = FieldOrdering.TryGetValue(dc.Operator, out int p) ? p : 20;
            return (priority, dc.Operator, dc.Date.ToString("s"));
        }
        if (cond is DurationCondition durc)
        {
            int priority = FieldOrdering.TryGetValue(durc.Operator, out int p) ? p : 20;
            return (priority, durc.Operator, durc.Duration);
        }
        if (cond is ExactMatchCondition emc)
        {
            return (18, "match", emc.Phrase);
        }
        if (cond is OrCondition orCond)
        {
            return (30, "or", orCond.ToGmailQuery(false));
        }
        return (99, string.Empty, cond.ToGmailQuery(false));
    }

    public string ToGmailQuery(bool explicitAnd = false)
    {
        if (Conditions.Count == 0) return string.Empty;
        if (Conditions.Count == 1) return Conditions[0].ToGmailQuery(explicitAnd);

        string separator = explicitAnd ? " AND " : " ";
        return string.Join(separator, Conditions.Select(c => FormatChild(c, explicitAnd)));
    }

    private static string FormatChild(IFilterCondition cond, bool explicitAnd)
    {
        string query = cond.ToGmailQuery(explicitAnd);
        // OrCondition with multiple elements needs parentheses when inside an AND
        if (cond is OrCondition orCond && orCond.Conditions.Count > 1)
        {
            return $"({query})";
        }
        return query;
    }

    public override string ToString() => ToGmailQuery();
}

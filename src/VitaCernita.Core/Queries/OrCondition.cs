using System;
using System.Collections.Generic;
using System.Linq;
using VitaCernita.Core.Filters;

namespace VitaCernita.Core.Queries;

/// <summary>
/// Disjunction (OR) of multiple query conditions.
/// </summary>
public sealed class OrCondition : IQueryCondition, IFilterCondition
{
    public IReadOnlyList<IQueryCondition> Conditions { get; }

    public OrCondition(IEnumerable<IQueryCondition> conditions)
    {
        if (conditions == null) throw new ArgumentNullException(nameof(conditions));

        // Flatten any nested OrConditions for associative canonical representation
        var list = new List<IQueryCondition>();
        foreach (var cond in conditions)
        {
            if (cond is OrCondition nestedOr)
            {
                list.AddRange(nestedOr.Conditions);
            }
            else
            {
                list.Add(cond);
            }
        }

        // Canonical deterministic sort based on standard query representation
        Conditions = list.OrderBy(c => c.ToGmailQuery(false), StringComparer.Ordinal).ToList();
    }

    public string ToGmailQuery(bool explicitAnd = false)
    {
        if (Conditions.Count == 0) return string.Empty;
        if (Conditions.Count == 1) return Conditions[0].ToGmailQuery(explicitAnd);

        return string.Join(" OR ", Conditions.Select(c => FormatChild(c, explicitAnd)));
    }

    private static string FormatChild(IQueryCondition cond, bool explicitAnd)
    {
        string query = cond.ToGmailQuery(explicitAnd);
        // AndCondition with multiple elements needs parentheses when inside an OR
        if (cond is AndCondition andCond && andCond.Conditions.Count > 1)
        {
            return $"({query})";
        }
        return query;
    }

    public override string ToString() => ToGmailQuery();
}

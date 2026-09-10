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
        { "subject", 3 }
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
            int priority = FieldOrdering.TryGetValue(fc.Field, out int p) ? p : 10;
            return (priority, fc.Field, fc.Value);
        }
        return (99, string.Empty, string.Empty);
    }

    public string ToGmailQuery(bool explicitAnd = false)
    {
        if (Conditions.Count == 0) return string.Empty;
        if (Conditions.Count == 1) return Conditions[0].ToGmailQuery(explicitAnd);

        string separator = explicitAnd ? " AND " : " ";
        return string.Join(separator, Conditions.Select(c => c.ToGmailQuery(explicitAnd)));
    }

    public override string ToString() => ToGmailQuery();
}

using System;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Queries.Validation;

namespace VitaCernita.Core.Queries;

/// <summary>
/// Exact word or phrase match condition (enclosed in double quotes per Gmail search spec).
/// </summary>
public sealed class ExactMatchCondition : IQueryCondition, IFilterCondition
{
    public string Phrase { get; }

    public ExactMatchCondition(string phrase)
    {
        QueryValidator.ValidateNonEmpty("match", phrase);
        Phrase = phrase.Trim();
    }

    public string ToGmailQuery(bool explicitAnd = false)
    {
        string p = Phrase;

        // Strip existing enclosing quotes if present
        if (p.Length >= 2 && p.StartsWith('"') && p.EndsWith('"'))
        {
            p = p[1..^1];
        }

        string escaped = p.Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }

    public override string ToString() => ToGmailQuery();
}

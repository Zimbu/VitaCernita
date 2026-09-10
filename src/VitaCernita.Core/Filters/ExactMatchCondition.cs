using System;
using VitaCernita.Core.Filters.Validation;

namespace VitaCernita.Core.Filters;

/// <summary>
/// Exact word or phrase match condition (enclosed in double quotes per Gmail search spec).
/// </summary>
public sealed class ExactMatchCondition : IFilterCondition
{
    public string Phrase { get; }

    public ExactMatchCondition(string phrase)
    {
        FilterValidator.ValidateNonEmpty("match", phrase);
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

using System;
using System.Collections.Generic;
using VitaCernita.Core.Filters;

namespace VitaCernita.Core.Diffing.Filters;

/// <summary>
/// Represents the computed difference between a current Gmail filter and a desired filter specification.
/// Pure model representation decoupled from Gmail API network or serialization concerns.
/// </summary>
public sealed class FilterDiff : ResourceDiff<GmailFilter>
{
    public GmailFilter? CurrentFilter => Current;
    public GmailFilter? DesiredFilter => Desired;

    public FilterDiff(
        string? id,
        string? name,
        DiffKind diffType,
        GmailFilter? currentFilter,
        GmailFilter? desiredFilter,
        IReadOnlyList<FieldDiff>? fieldDifferences = null)
        : base(diffType, currentFilter, desiredFilter, fieldDifferences, identifier: name, id: id)
    {
    }

    public override string ToString()
    {
        string idInfo = Id != null ? $" (ID: {Id})" : "";
        string nameInfo = !string.IsNullOrWhiteSpace(Identifier) ? $" '{Identifier}'" : "";

        return DiffType switch
        {
            DiffKind.Added => $"+ Filter{nameInfo}{idInfo} (Create): query='{Desired?.ToGmailQuery()}' action={Desired?.Action}",
            DiffKind.Removed => $"- Filter{nameInfo}{idInfo} (Delete)",
            DiffKind.Modified => $"~ Filter{nameInfo}{idInfo} ({FieldDifferences.Count} change(s): {string.Join(", ", FieldDifferences)})",
            _ => $"Filter{nameInfo}{idInfo} (Unchanged)"
        };
    }
}

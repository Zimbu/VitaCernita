using System;
using System.Collections.Generic;
using System.Linq;

namespace VitaCernita.Core.Filters.Diff;

/// <summary>
/// Represents the computed difference between a current Gmail filter and a desired filter specification.
/// </summary>
public sealed class FilterDiff
{
    public string? Id { get; }
    public string? Name { get; }
    public FilterDiffType DiffType { get; }
    public GmailFilter? CurrentFilter { get; }
    public GmailFilter? DesiredFilter { get; }
    public IReadOnlyList<FilterFieldDiff> FieldDifferences { get; }

    public bool HasChanges => DiffType != FilterDiffType.Unchanged;

    public FilterDiff(
        string? id,
        string? name,
        FilterDiffType diffType,
        GmailFilter? currentFilter,
        GmailFilter? desiredFilter,
        IReadOnlyList<FilterFieldDiff>? fieldDifferences = null)
    {
        Id = id;
        Name = name;
        DiffType = diffType;
        CurrentFilter = currentFilter;
        DesiredFilter = desiredFilter;
        FieldDifferences = fieldDifferences ?? Array.Empty<FilterFieldDiff>();
    }

    /// <summary>
    /// Generates a JSON-ready dictionary payload for a Gmail API users.settings.filters.create call.
    /// </summary>
    public Dictionary<string, object>? GetCreatePayload(bool explicitAnd = false)
    {
        return DesiredFilter?.ToDictionary(explicitAnd);
    }

    /// <summary>
    /// Returns the target filter ID to delete for a Gmail API users.settings.filters.delete call.
    /// </summary>
    public string? GetDeleteId()
    {
        return CurrentFilter?.Id ?? Id;
    }

    public override string ToString()
    {
        string idInfo = Id != null ? $" (ID: {Id})" : "";
        string nameInfo = !string.IsNullOrWhiteSpace(Name) ? $" '{Name}'" : "";

        return DiffType switch
        {
            FilterDiffType.Added => $"+ Filter{nameInfo}{idInfo} (Create): query='{DesiredFilter?.ToGmailQuery()}' action={DesiredFilter?.Action}",
            FilterDiffType.Removed => $"- Filter{nameInfo}{idInfo} (Delete): query='{CurrentFilter?.ToGmailQuery()}'",
            FilterDiffType.Modified => $"~ Filter{nameInfo}{idInfo} ({FieldDifferences.Count} change(s): {string.Join(", ", FieldDifferences)})",
            _ => $"  Filter{nameInfo}{idInfo} (Unchanged)"
        };
    }
}

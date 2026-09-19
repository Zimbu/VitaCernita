using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VitaCernita.Core.Filters;

namespace VitaCernita.Core.Diffing.Filters;

/// <summary>
/// Represents the computed differences between two collections of Gmail filters.
/// </summary>
public sealed class FilterSetDiff : ResourceSetDiff<GmailFilter>
{
    public new IReadOnlyList<FilterDiff> Differences { get; }
    public new IReadOnlyList<FilterDiff> Creations => Differences.Where(d => d.DiffType == DiffKind.Added).ToList();
    public new IReadOnlyList<FilterDiff> Deletions => Differences.Where(d => d.DiffType == DiffKind.Removed).ToList();
    public new IReadOnlyList<FilterDiff> Modifications => Differences.Where(d => d.DiffType == DiffKind.Modified).ToList();
    public new IReadOnlyList<FilterDiff> Unchanged => Differences.Where(d => d.DiffType == DiffKind.Unchanged).ToList();

    public FilterSetDiff(IEnumerable<FilterDiff>? differences)
        : base((differences ?? Array.Empty<FilterDiff>()).Cast<ResourceDiff<GmailFilter>>())
    {
        Differences = (differences ?? Array.Empty<FilterDiff>()).ToList();
    }

    public string ToSummaryString()
    {
        return $"Summary: {TotalCreations} to create, {TotalModifications} to update, {TotalDeletions} to delete, {TotalUnchanged} unchanged.";
    }

    /// <summary>
    /// Generates a comprehensive, human-readable dry-run report of filter differences.
    /// </summary>
    public string ToDryRunReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("======================================================================");
        sb.AppendLine("VitaCernita Filter Diff Report (Dry Run)");
        sb.AppendLine("======================================================================");
        sb.AppendLine(ToSummaryString());
        sb.AppendLine();

        if (TotalCreations > 0)
        {
            sb.AppendLine($"[+] Create ({TotalCreations}):");
            foreach (var item in Creations)
            {
                string nameStr = !string.IsNullOrWhiteSpace(item.Identifier) ? $" '{item.Identifier}'" : "";
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                string queryStr = item.DesiredFilter?.ToGmailQuery() ?? "<none>";
                string actionStr = item.DesiredFilter?.Action?.ToString() ?? "<none>";
                sb.AppendLine($"  + Filter{nameStr}{idStr}:");
                sb.AppendLine($"      Query : {queryStr}");
                sb.AppendLine($"      Action: {actionStr}");
            }
            sb.AppendLine();
        }

        if (TotalModifications > 0)
        {
            sb.AppendLine($"[~] Update ({TotalModifications}):");
            foreach (var item in Modifications)
            {
                string nameStr = !string.IsNullOrWhiteSpace(item.Identifier) ? $" '{item.Identifier}'" : "";
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                sb.AppendLine($"  ~ Filter{nameStr}{idStr}:");
                foreach (var fieldDiff in item.FieldDifferences)
                {
                    sb.AppendLine($"      * {fieldDiff}");
                }
            }
            sb.AppendLine();
        }

        if (TotalDeletions > 0)
        {
            sb.AppendLine($"[-] Delete ({TotalDeletions}):");
            foreach (var item in Deletions)
            {
                string nameStr = !string.IsNullOrWhiteSpace(item.Identifier) ? $" '{item.Identifier}'" : "";
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                string queryStr = item.CurrentFilter?.ToGmailQuery() ?? "<none>";
                sb.AppendLine($"  - Filter{nameStr}{idStr}: query '{queryStr}'");
            }
            sb.AppendLine();
        }

        if (TotalUnchanged > 0)
        {
            sb.AppendLine($"[=] Unchanged ({TotalUnchanged}):");
            foreach (var item in Unchanged)
            {
                string nameStr = !string.IsNullOrWhiteSpace(item.Identifier) ? $" '{item.Identifier}'" : "";
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                sb.AppendLine($"  = Filter{nameStr}{idStr}");
            }
            sb.AppendLine();
        }

        sb.Append("======================================================================");
        return sb.ToString();
    }

    public override string ToString() => ToSummaryString();
}

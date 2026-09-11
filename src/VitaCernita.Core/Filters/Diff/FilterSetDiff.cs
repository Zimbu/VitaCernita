using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VitaCernita.Core.Filters.Diff;

/// <summary>
/// Represents the complete diff result between a set of current Gmail filters and a set of desired filters.
/// Suitable for both programmatic synchronization planning and human-readable dry-run reporting.
/// </summary>
public sealed class FilterSetDiff
{
    public IReadOnlyList<FilterDiff> Differences { get; }

    public IReadOnlyList<FilterDiff> Creations => Differences.Where(d => d.DiffType == FilterDiffType.Added).ToList();
    public IReadOnlyList<FilterDiff> Deletions => Differences.Where(d => d.DiffType == FilterDiffType.Removed).ToList();
    public IReadOnlyList<FilterDiff> Modifications => Differences.Where(d => d.DiffType == FilterDiffType.Modified).ToList();
    public IReadOnlyList<FilterDiff> Unchanged => Differences.Where(d => d.DiffType == FilterDiffType.Unchanged).ToList();

    public bool HasDifferences => Creations.Count > 0 || Deletions.Count > 0 || Modifications.Count > 0;
    public int TotalCreations => Creations.Count;
    public int TotalDeletions => Deletions.Count;
    public int TotalModifications => Modifications.Count;
    public int TotalUnchanged => Unchanged.Count;

    public FilterSetDiff(IEnumerable<FilterDiff>? differences)
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
                string nameStr = !string.IsNullOrWhiteSpace(item.Name) ? $" '{item.Name}'" : "";
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
                string nameStr = !string.IsNullOrWhiteSpace(item.Name) ? $" '{item.Name}'" : "";
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
                string nameStr = !string.IsNullOrWhiteSpace(item.Name) ? $" '{item.Name}'" : "";
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
                string nameStr = !string.IsNullOrWhiteSpace(item.Name) ? $" '{item.Name}'" : "";
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VitaCernita.Core.Labels.Diff;

/// <summary>
/// Represents the complete diff result between a set of current Gmail labels and a set of desired labels.
/// Suitable for both programmatic command dispatching and dry-run reporting.
/// </summary>
public sealed class LabelSetDiff
{
    public IReadOnlyList<LabelDiff> Differences { get; }

    public IReadOnlyList<LabelDiff> Creations => Differences.Where(d => d.DiffType == LabelDiffType.Added).ToList();
    public IReadOnlyList<LabelDiff> Deletions => Differences.Where(d => d.DiffType == LabelDiffType.Removed).ToList();
    public IReadOnlyList<LabelDiff> Modifications => Differences.Where(d => d.DiffType == LabelDiffType.Modified).ToList();
    public IReadOnlyList<LabelDiff> Unchanged => Differences.Where(d => d.DiffType == LabelDiffType.Unchanged).ToList();

    public bool HasDifferences => Creations.Count > 0 || Deletions.Count > 0 || Modifications.Count > 0;
    public int TotalCreations => Creations.Count;
    public int TotalDeletions => Deletions.Count;
    public int TotalModifications => Modifications.Count;
    public int TotalUnchanged => Unchanged.Count;

    public LabelSetDiff(IEnumerable<LabelDiff> differences)
    {
        Differences = (differences ?? Array.Empty<LabelDiff>()).ToList();
    }

    public string ToSummaryString()
    {
        return $"Summary: {TotalCreations} to create, {TotalModifications} to update, {TotalDeletions} to delete, {TotalUnchanged} unchanged.";
    }

    /// <summary>
    /// Generates a comprehensive, human-readable dry-run report.
    /// </summary>
    public string ToDryRunReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("======================================================================");
        sb.AppendLine("VitaCernita Label Diff Report (Dry Run)");
        sb.AppendLine("======================================================================");
        sb.AppendLine(ToSummaryString());
        sb.AppendLine();

        if (TotalCreations > 0)
        {
            sb.AppendLine($"[+] Create ({TotalCreations}):");
            foreach (var item in Creations)
            {
                var label = item.DesiredLabel;
                var details = new List<string>();
                if (!string.IsNullOrWhiteSpace(label?.MessageListVisibility))
                    details.Add($"MessageList: {label.MessageListVisibility}");
                if (!string.IsNullOrWhiteSpace(label?.LabelListVisibility))
                    details.Add($"LabelList: {label.LabelListVisibility}");
                if (label?.Color != null)
                    details.Add($"Color: [{label.Color.TextColor} / {label.Color.BackgroundColor}]");

                string detailStr = details.Count > 0 ? $" ({string.Join(", ", details)})" : "";
                sb.AppendLine($"  + '{item.Name}'{detailStr}");
            }
            sb.AppendLine();
        }

        if (TotalModifications > 0)
        {
            sb.AppendLine($"[~] Update ({TotalModifications}):");
            foreach (var item in Modifications)
            {
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                sb.AppendLine($"  ~ '{item.Name}'{idStr}:");
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
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                sb.AppendLine($"  - '{item.Name}'{idStr}");
            }
            sb.AppendLine();
        }

        if (TotalUnchanged > 0)
        {
            sb.AppendLine($"[=] Unchanged ({TotalUnchanged}):");
            foreach (var item in Unchanged)
            {
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                sb.AppendLine($"  = '{item.Name}'{idStr}");
            }
            sb.AppendLine();
        }

        sb.Append("======================================================================");
        return sb.ToString();
    }

    public override string ToString() => ToSummaryString();
}
